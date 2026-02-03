using System;
using System.Threading;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Save;
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

            // 4) 런타임 적용(바인딩 Apply)
            ApplyToRuntime(data);

            LoadedProgress = data;

            // 5) SavePresenter 초기화 + Arm
            savePresenter.SetSlotId(CurrentSlotId);
            savePresenter.InitializeCache(data);
            savePresenter.Arm();

            if (log)
                Debug.Log($"[CombatBoot] READY userId={CurrentUserId} slot={CurrentSlotId} stage={data.stageIndex} gold={data.gold}");
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
    }
}
