using System;
using System.Threading;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Offline;
using MyGame.Application.Save;
using MyGame.Combat;
using MyGame.Presentation.Progress;

namespace MyGame.Presentation.Combat
{
    /// <summary>
    /// Combat 씬 진입 부팅 Presenter
    /// 1) 세션 보장(SignIn 필요 시 수행)
    /// 2) userId 기반 슬롯ID 결정(유저 폴더 분리)
    /// 3) 진행 데이터 로드 → 바인딩 Apply
    /// 4) SavePresenter 캐시 초기화 → Arm
    /// </summary>
    public sealed class CombatBootPresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private PlayerProgressSavePresenter savePresenter;

        [Header("Slot Convention")]
        [Tooltip("유저 진행 데이터 슬롯 접두어 (폴더명 역할)")]
        [SerializeField] private string playerSlotPrefix = "player";

        [Header("Debug")]
        [SerializeField] private bool log = true;

        [Header("Offline Settlement (Idle/AFK)")]
        [SerializeField] private ScriptableObject offlineBalanceTableSource;
        [SerializeField, Range(1, 12)] private int offlineHourCap = 12;

        [Header("Offline Tier Source")]
        [SerializeField] private bool useAutoCombatPowerTier = true;
        [SerializeField] private ActorStats combatPowerSourceStats;
        [SerializeField, Min(0)] private int manualCombatPowerTier = 0;

        [Header("Auto Tier Rule")]
        [SerializeField, Min(1)] private int powerPerTier = 1000;
        [SerializeField, Range(1, 10)] private int maxAutoTierCount = 10;
        [SerializeField, Min(0)] private int maxOfflineBalanceTierIndex = 4;

        private IOfflineAfkBalanceSource OfflineBalanceTable
            => offlineBalanceTableSource as IOfflineAfkBalanceSource;

        public PlayerProgressSaveData LoadedProgress { get; private set; }
        public string CurrentUserId { get; private set; }
        public string CurrentSlotId { get; private set; }

        private async void Start()
        {
            if (savePresenter == null)
            {
                Debug.LogError("[CombatBoot] PlayerProgressSavePresenter is not assigned.");
                return;
            }

            // 부팅 중 저장 방지
            savePresenter.Disarm();
            savePresenter.RebuildBindingsCache();

            if (App.Auth == null)
            {
                Debug.LogError("[CombatBoot] App.Auth is null. Check AppAutoBootstrap/AppCompositionRoot.");
                return;
            }

            if (App.Save == null)
            {
                Debug.LogWarning("[CombatBoot] App.Save is null. Boot skipped with defaults.");
                var defaults = new PlayerProgressSaveData();
                ApplyDefaultsFromRuntime(defaults);

                LoadedProgress = defaults;
                savePresenter.InitializeCache(defaults);
                savePresenter.Arm();
                return;
            }

            // 1) 세션 보장 (Combat 씬 단독 실행 대비)
            var session = App.Auth.Current;
            if (!session.IsSignedIn)
                session = await App.Auth.SignInAsync(CancellationToken.None);

            CurrentUserId = session.UserId;

            // 2) 슬롯 결정 (V2: 폴더 분리)
            string v2SlotId = BuildPlayerSlotIdV2(CurrentUserId);

            // (옵션) 레거시 슬롯(이전에 사용하던 방식)도 한 번 체크해서 데이터 유실 방지
            string legacySlotId = BuildPlayerSlotIdLegacy(CurrentUserId);

            // 3) 로드 (먼저 V2 시도)
            var result = await App.Save.LoadAsync<PlayerProgressSaveData>(
                v2SlotId,
                PlayerProgressSaveData.TypeId,
                true,                  // autoResaveAfterMigration
                CancellationToken.None
            );

            PlayerProgressSaveData data;

            if (result.Success && result.Data != null)
            {
                CurrentSlotId = v2SlotId;
                data = result.Data;
            }
            else if (result.Status == SaveLoadStatus.NotFound)
            {
                // V2가 없으면 레거시를 확인
                var legacy = await App.Save.LoadAsync<PlayerProgressSaveData>(
                    legacySlotId,
                    PlayerProgressSaveData.TypeId,
                    true,
                    CancellationToken.None
                );

                if (legacy.Success && legacy.Data != null)
                {
                    // 레거시 데이터를 V2 위치로 복사 저장(마이그레이션)
                    CurrentSlotId = v2SlotId;
                    data = legacy.Data;

                    var migSave = await App.Save.SaveAsync(
                        v2SlotId,
                        data,
                        PlayerProgressSaveData.TypeId,
                        CancellationToken.None
                    );

                    if (log)
                        Debug.Log($"[CombatBoot] MIGRATE legacy -> v2 success={migSave.Success} status={migSave.Status}");
                }
                else
                {
                    // 둘 다 없으면 defaults 생성
                    CurrentSlotId = v2SlotId;
                    data = new PlayerProgressSaveData();
                    ApplyDefaultsFromRuntime(data);

                    var firstSave = await App.Save.SaveAsync(
                        CurrentSlotId,
                        data,
                        PlayerProgressSaveData.TypeId,
                        CancellationToken.None
                    );

                    if (log)
                        Debug.Log($"[CombatBoot] CREATE default save success={firstSave.Success} status={firstSave.Status}");
                }
            }
            else
            {
                // NotFound 외의 오류(손상/타입불일치 등)면 defaults로 안전 부팅
                CurrentSlotId = v2SlotId;
                data = new PlayerProgressSaveData();
                ApplyDefaultsFromRuntime(data);

                if (log)
                    Debug.LogWarning($"[CombatBoot] LOAD failed status={result.Status} msg={result.Message}. Boot with defaults.");
            }

            // 4) 오프라인 정산 반영 (3요소: 12캡 + Stage×Tier 2D + 드랍 소수점 carry)
            bool offlineApplied = ApplyOfflineSettlement(data);

            // 5) 런타임 적용(바인딩 Apply)
            ApplyToRuntime(data);

            LoadedProgress = data;

            // 6) SavePresenter 초기화 + Arm
            savePresenter.SetSlotId(CurrentSlotId);
            savePresenter.InitializeCache(data);
            savePresenter.Arm();

            // 부팅 시 오프라인 정산이 발생했다면 즉시 1회 저장
            if (offlineApplied)
            {
                var settleSave = await App.Save.SaveAsync(
                    CurrentSlotId,
                    data,
                    PlayerProgressSaveData.TypeId,
                    CancellationToken.None
                );

                if (log)
                    Debug.Log($"[CombatBoot] Offline settlement saved success={settleSave.Success} status={settleSave.Status}");
            }

            if (log)
                Debug.Log($"[CombatBoot] READY userId={CurrentUserId} slot={CurrentSlotId} stage={data.stageIndex} gold={data.gold} gem={data.gem}");
        }

        private bool ApplyOfflineSettlement(PlayerProgressSaveData data)
        {
            if (data == null) return false;

            long nowTicks = DateTime.UtcNow.Ticks;

            // 첫 진입이면 기준시각만 기록
            if (data.lastSeenUtcTicks <= 0)
            {
                data.lastSeenUtcTicks = nowTicks;
                return false;
            }

            long elapsedSeconds = Math.Max(0L, (nowTicks - data.lastSeenUtcTicks) / TimeSpan.TicksPerSecond);
            data.lastSeenUtcTicks = nowTicks;

            if (elapsedSeconds <= 0) return false;
            if (OfflineBalanceTable == null)
            {
                if (log) Debug.LogWarning("[CombatBoot] offlineBalanceTableSource is null or does not implement IOfflineAfkBalanceSource. Skip offline settlement.");
                return false;
            }

            var rule = new OfflineAfkRule { maxHoursCap = Mathf.Clamp(offlineHourCap, 1, 12) };
            int resolvedPowerTier = ResolveOfflinePowerTier();
            var input = new OfflineAfkInput
            {
                elapsedSeconds = elapsedSeconds,
                stageIndex = Mathf.Max(1, data.stageIndex),
                powerTier = resolvedPowerTier,
                dropCarry = Math.Max(0d, data.offlineDropCarry)
            };

            OfflineAfkResult result = OfflineAfkCalculator.Compute(
                in rule,
                in input,
                (stage, tier) => OfflineBalanceTable.ResolveCell(stage, tier)
            );

            data.gold += result.gold;
            data.offlineExpClaimable += result.exp;
            data.offlineDropClaimable += result.dropCount;
            data.offlineDropCarry = result.nextDropCarry;

            if (log)
            {
                Debug.Log(
                    $"[OfflineAFK] elapsed={elapsedSeconds}s capped={result.cappedSeconds}s " +
                    $"tier={resolvedPowerTier} " +
                    $"reward(gold={result.gold}, expClaim={result.exp}, dropClaim={result.dropCount}, carry={result.nextDropCarry:0.####})");
            }

            return result.HasReward;
        }

        // ---------------------------------
        // 슬롯 규칙
        // ---------------------------------

        //  V2: 폴더 분리 (key에 / 포함 → JsonFileSaveStore가 중간 폴더 생성해야 함)
        // 실제 파일 키는 SaveService.DefaultKey가 "save_"를 붙이므로:
        // Saves/save_player/<userId>/progress_0.json 형태가 된다.
        private string BuildPlayerSlotIdV2(string userId)
            => $"{playerSlotPrefix}/{userId}/progress_0";

        //  레거시(예전): 한 파일로만
        private string BuildPlayerSlotIdLegacy(string userId)
            => $"{playerSlotPrefix}_{userId}_0";

        // ---------------------------------
        // 바인딩 Apply / Capture (SettingsBootPresenter와 동일)
        // ---------------------------------
        private void ApplyToRuntime(PlayerProgressSaveData data)
        {
            //  savePresenter 기준 트리에서 바인딩을 찾는다(구조에 덜 민감)
            var root = savePresenter != null ? savePresenter.transform : transform;

            var monos = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] is IPlayerProgressBinding b)
                    b.ApplyFromSave(data);
            }
        }

        private void ApplyDefaultsFromRuntime(PlayerProgressSaveData data)
        {
            var root = savePresenter != null ? savePresenter.transform : transform;

            var monos = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] is IPlayerProgressBinding b)
                    b.CaptureToSave(data);
            }
        }

        private int ResolveOfflinePowerTier()
        {
            if (!useAutoCombatPowerTier)
                return Mathf.Clamp(manualCombatPowerTier, 0, Math.Max(0, maxOfflineBalanceTierIndex));

            ActorStats source = ResolveCombatPowerSource();
            if (source == null)
            {
                if (log)
                    Debug.LogWarning("[CombatBoot] Auto tier enabled but combatPowerSourceStats not found. Fallback to manualCombatPowerTier.");

                return Mathf.Clamp(manualCombatPowerTier, 0, Math.Max(0, maxOfflineBalanceTierIndex));
            }

            int score = ComputeCombatPowerScore(source);
            int maxAutoTierIndex = Math.Max(0, Mathf.Clamp(maxAutoTierCount, 1, 10) - 1);
            int autoTierIndex = Mathf.Clamp(score / Math.Max(1, powerPerTier), 0, maxAutoTierIndex);
            int tableTierIndex = Mathf.Clamp(autoTierIndex, 0, Math.Max(0, maxOfflineBalanceTierIndex));

            if (log)
            {
                Debug.Log(
                    $"[CombatBoot] Auto power tier score={score} autoTier(index={autoTierIndex}, human={autoTierIndex + 1}) " +
                    $"tableTier(index={tableTierIndex}, human={tableTierIndex + 1})");
            }

            return tableTierIndex;
        }

        private ActorStats ResolveCombatPowerSource()
        {
            if (combatPowerSourceStats != null)
                return combatPowerSourceStats;

            Actor[] actors = FindObjectsOfType<Actor>(true);
            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i] == null || actors[i].kind != ActorKind.Player)
                    continue;

                if (actors[i].Stats != null)
                {
                    combatPowerSourceStats = actors[i].Stats;
                    return combatPowerSourceStats;
                }
            }

            return null;
        }

        private static int ComputeCombatPowerScore(ActorStats stats)
        {
            // 환산 규칙(요청안):
            // AP/60 + AC/5 + AS/1 + MP/720 + MA/5 + MD/60 + HP/720 + DP/60 + HV/5 + BF/1 + TA/5 + LK/5
            double score = 0d;
            score += stats.GetBaseFinalStat(StatId.AP) / 60d;
            score += stats.GetBaseFinalStat(StatId.AC) / 5d;
            score += stats.GetBaseFinalStat(StatId.AS) / 1d;

            score += stats.GetBaseFinalStat(StatId.MP) / 720d;
            score += stats.GetBaseFinalStat(StatId.MA) / 5d;
            score += stats.GetBaseFinalStat(StatId.MD) / 60d;

            score += stats.GetBaseFinalStat(StatId.HP) / 720d;
            score += stats.GetBaseFinalStat(StatId.DP) / 60d;
            score += stats.GetBaseFinalStat(StatId.HV) / 5d;

            score += stats.GetBaseFinalStat(StatId.BF) / 1d;
            score += stats.GetBaseFinalStat(StatId.TA) / 5d;
            score += stats.GetBaseFinalStat(StatId.LK) / 5d;

            return Math.Max(0, (int)Math.Floor(score));
        }
    }
}
