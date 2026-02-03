using System;
using System.Threading;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Save;
using MyGame.Application.Auth;

namespace MyGame.Presentation.Combat
{
    /// <summary>
    ///  Combat 씬 진입 시 "유저 세션 보장 + 유저 데이터 로드" 담당 Presenter
    /// - Title을 거치지 않고 Combat 씬만 실행해도 안전하게 부팅되도록 한다.
    /// - userId를 '실제로 쓰는' 첫 지점: slotId를 userId 기반으로 결정해 로드한다.
    /// </summary>
    public sealed class CombatBootPresenter : MonoBehaviour
    {
        [Header("Slot Convention")]
        [Tooltip("유저 진행 데이터 슬롯 접두어")]
        [SerializeField] private string playerSlotPrefix = "player";

        [Header("Debug")]
        [SerializeField] private bool log = true;

        public PlayerProgressSaveData LoadedProgress { get; private set; }
        public string CurrentUserId { get; private set; }

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            FireAndForget(BootAsync(_cts.Token));
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async System.Threading.Tasks.Task BootAsync(CancellationToken ct)
        {
            // 1) App 초기화 체크
            if (App.Auth == null)
            {
                Debug.LogError("[CombatBoot] App.Auth is null. Check AppAutoBootstrap/AppCompositionRoot.");
                return;
            }

            if (App.Save == null)
            {
                Debug.LogError("[CombatBoot] App.Save is null. Save system is not initialized.");
                return;
            }

            // 2) 세션 보장 (Combat 씬 단독 실행 대비)
            var session = App.Auth.Current;
            if (!session.IsSignedIn)
                session = await App.Auth.SignInAsync(ct);

            CurrentUserId = session.UserId;

            // 3) userId 기반 슬롯 결정
            string slotId = BuildPlayerSlotId(CurrentUserId);

            // 4) 로드
            var load = await App.Save.LoadAsync<PlayerProgressSaveData>(
                slotId,
                PlayerProgressSaveData.TypeId,
                autoResaveAfterMigration: true,
                ct: ct
            );

            if (load.Success && load.Data != null)
            {
                LoadedProgress = load.Data;
                if (log) Debug.Log($"[CombatBoot] LOAD OK userId={CurrentUserId} slot={slotId} stage={LoadedProgress.stageIndex} gold={LoadedProgress.gold}");
                return;
            }

            // 5) 없으면 기본값 생성 + 저장(첫 유저)
            LoadedProgress = new PlayerProgressSaveData();
            if (log) Debug.Log($"[CombatBoot] No save. Create default userId={CurrentUserId} slot={slotId}");

            var save = await App.Save.SaveAsync(
                slotId,
                LoadedProgress,
                PlayerProgressSaveData.TypeId,
                ct
            );

            if (log) Debug.Log($"[CombatBoot] SAVE default success={save.Success} status={save.Status} slot={slotId}");
        }

        private string BuildPlayerSlotId(string userId)
            => $"{playerSlotPrefix}_{userId}_0";

        private static async void FireAndForget(System.Threading.Tasks.Task task)
        {
            try { await task; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
