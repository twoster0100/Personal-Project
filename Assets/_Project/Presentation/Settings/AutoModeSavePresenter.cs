using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Save;

namespace MyGame.Presentation.Settings
{
    /// <summary>
    /// AutoMode 저장 담당.
    /// - Arm() 이후부터만 저장한다 (부팅 중 저장 방지)
    /// - UI 입력이 들어오면 NotifyChangedFromUi()로 Dirty -> 디바운스 저장
    /// - Pause/Quit/Disable 시 dirty면 플러시(보험)
    /// </summary>
    public sealed class AutoModeSavePresenter : MonoBehaviour
    {
        [Header("Source of truth (Model)")]
        [SerializeField] private AutoModeController autoMode;

        [Header("Save Slot")]
        [SerializeField] private string slotId = "0";

        [Header("Debounce (seconds)")]
        [SerializeField] private float debounceSeconds = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool log = false;

        private bool _armed;
        private bool _dirty;
        private CancellationTokenSource _debounceCts;

        private void Awake()
        {
            _armed = false;
            _dirty = false;
        }

        public void Arm()
        {
            _armed = true;
            if (log) Debug.Log($"[SettingsSave] Arm slot={slotId}");
        }

        public void Disarm()
        {
            _armed = false;
            _dirty = false;
            CancelDebounce();
            if (log) Debug.Log("[SettingsSave] Disarm");
        }

        /// <summary>
        /// ✅ UI 터치(클릭)로 변경이 확정됐음을 통지
        /// </summary>
        public void NotifyChangedFromUi()
        {
            if (!_armed) return;
            if (autoMode == null)
            {
                Debug.LogError("[SettingsSave] AutoModeController is not assigned.");
                return;
            }

            _dirty = true;
            if (log) Debug.Log($"[SettingsSave] Dirty by UI. autoMode={autoMode.IsAuto}");
            ScheduleDebouncedSave();
        }

        private void OnDisable()
        {
            // 에디터 Stop에서도 보험 저장(Dirty일 때만)
            if (_armed && _dirty)
                FireAndForget(SaveNowAsync(force: false, CancellationToken.None));

            CancelDebounce();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) return;
            if (_armed && _dirty)
                FireAndForget(SaveNowAsync(force: false, CancellationToken.None));
        }

        private void OnApplicationQuit()
        {
            if (_armed && _dirty)
                FireAndForget(SaveNowAsync(force: true, CancellationToken.None));
        }

        private void ScheduleDebouncedSave()
        {
            CancelDebounce();

            _debounceCts = new CancellationTokenSource();
            FireAndForget(DebouncedSaveAsync(_debounceCts.Token));
        }

        private async Task DebouncedSaveAsync(CancellationToken token)
        {
            try
            {
                int ms = Mathf.RoundToInt(debounceSeconds * 1000f);
                if (ms > 0)
                    await Task.Delay(ms, token);

                await SaveNowAsync(force: false, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }

        private async Task SaveNowAsync(bool force, CancellationToken token)
        {
            if (!_armed) return;
            if (!force && !_dirty) return;
            if (autoMode == null) return;

            if (App.Save == null)
            {
                if (log) Debug.LogWarning("[SettingsSave] App.Save is null. Skip.");
                return;
            }

            var data = new PrototypeSaveData { autoMode = autoMode.IsAuto };

            var result = await App.Save.SaveAsync(
                slotId,
                data,
                PrototypeSaveData.TypeId,
                token);

            if (result.Success)
                _dirty = false;

            if (log)
                Debug.Log($"[SettingsSave] success={result.Success} status={result.Status} autoMode={data.autoMode} slot={slotId}");
        }

        private void CancelDebounce()
        {
            if (_debounceCts == null) return;

            try { _debounceCts.Cancel(); }
            finally
            {
                _debounceCts.Dispose();
                _debounceCts = null;
            }
        }

        private static async void FireAndForget(Task task)
        {
            try { await task; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
