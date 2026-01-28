using System.Threading;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Save;

namespace MyGame.Presentation.Settings
{
    public sealed class AutoModeSavePresenter : MonoBehaviour
    {
        [Header("Source of truth (Model)")]
        [SerializeField] private AutoModeController _autoMode;  

        [Header("Debounce")]
        [SerializeField] private float _debounceSeconds = 1.0f;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            if (_autoMode == null)
            {
                Debug.LogError("[SettingsSave] AutoModeController is not assigned.");
                return;
            }

            _autoMode.onAutoChanged.AddListener(OnAutoChanged);
            // 시작 상태도 파일에 반영하고 싶으면 아래 한 줄 켜도 됨
            // _ = SaveNow();
        }

        private void OnDisable()
        {
            if (_autoMode != null)
                _autoMode.onAutoChanged.RemoveListener(OnAutoChanged);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) _ = SaveNow();
        }

        private void OnApplicationQuit()
        {
            _ = SaveNow();
        }

        private void OnAutoChanged(bool isOn)
        {
            // 디바운스: 연속 변경 시 마지막 상태만 저장
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _ = SaveAfterDelay(_cts.Token);
        }

        private async System.Threading.Tasks.Task SaveAfterDelay(CancellationToken ct)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay((int)(_debounceSeconds * 1000f), ct);
                await SaveNow();
            }
            catch (System.OperationCanceledException) { }
        }

        private async System.Threading.Tasks.Task SaveNow()
        {
            if (App.Save == null) return;
            if (_autoMode == null) return;

            var data = new PrototypeSaveData
            {
                autoMode = _autoMode.IsAuto,
                targetFpsMode = 0,
                stageIndex = 1,
                gold = 123
            };

            var r = await App.Save.SaveAsync("0", data, PrototypeSaveData.TypeId);
            Debug.Log($"[SettingsSave] success={r.Success} status={r.Status}");
        }
    }
}
