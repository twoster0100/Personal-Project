using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MyGame.Application;       // App.Save
using MyGame.Application.Save;  // SettingsSaveData

namespace MyGame.Presentation.Settings
{
    /// <summary>
    /// "설정 저장" 공통 Presenter.
    /// - UI/모델 변경이 일어나면 NotifyChangedFromUi()만 호출하면 됨
    /// - 실제 저장 데이터는 ISettingsBinding들로부터 수집(CaptureToSave)해서 저장
    /// - Debounce + Pause/Quit/Disable 플러시(보험)
    /// </summary>
    public sealed class SettingsSavePresenter : MonoBehaviour
    {
        [Header("Save Slot (settings_0 권장)")]
        [SerializeField] private string slotId = "settings_0";

        [Header("Bindings Root (비워두면 자기 자신부터 탐색)")]
        [SerializeField] private Transform bindingsRoot;

        [Header("Debounce (seconds)")]
        [SerializeField] private float debounceSeconds = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool log = false;

        private bool _armed;
        private bool _dirty;

        private readonly List<ISettingsBinding> _bindings = new(16);
        private SettingsSaveData _cache = new();

        private CancellationTokenSource _debounceCts;

        public string SlotId => slotId;

        private void Awake()
        {
            RebuildBindingsCache();
        }

        public void RebuildBindingsCache()
        {
            _bindings.Clear();

            var root = bindingsRoot != null ? bindingsRoot : transform;
            var monos = root.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] is ISettingsBinding b)
                    _bindings.Add(b);
            }

            if (log) Debug.Log($"[SettingsSave] Bindings={_bindings.Count}");
        }

        /// <summary>부팅 완료 후 호출: 이제부터 저장 가능</summary>
        public void Arm()
        {
            _armed = true;
            if (log) Debug.Log("[SettingsSave] Arm");
        }

        /// <summary>부팅 시작/리셋 시 호출: 저장 금지 + pending 저장 취소</summary>
        public void Disarm()
        {
            _armed = false;
            _dirty = false;
            CancelDebounce();
            if (log) Debug.Log("[SettingsSave] Disarm");
        }

        /// <summary>
        /// 부팅 로드 결과를 캐시에 주입. (없으면 defaults)
        /// </summary>
        public void InitializeCache(SettingsSaveData loadedOrDefault)
        {
            _cache = loadedOrDefault ?? new SettingsSaveData();
        }

        /// <summary>
        /// UI 입력 Presenter가 호출하는 유일한 엔트리.
        /// "내가 설정을 바꿨으니 저장해줘!"
        /// </summary>
        public void NotifyChangedFromUi(string reason = null)
        {
            if (!_armed) return;

            _dirty = true;

            if (log)
                Debug.Log($"[SettingsSave] Dirty by UI. reason={reason ?? "null"}");

            ScheduleDebouncedSave();
        }

        private void OnDisable()
        {
            // 에디터 Stop은 Quit이 안 올 수 있음 → dirty일 때만 보험 저장
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
            var token = _debounceCts.Token;

            FireAndForget(DebouncedSaveAsync(token));
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
            catch (OperationCanceledException)
            {
                // 정상: 디바운스 취소
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async Task SaveNowAsync(bool force, CancellationToken token)
        {
            if (!_armed) return;
            if (!force && !_dirty) return;

            if (App.Save == null)
            {
                if (log) Debug.LogWarning("[SettingsSave] App.Save is null. Skip.");
                return;
            }

            // 최신 모델값을 캐시에 다시 수집
            for (int i = 0; i < _bindings.Count; i++)
                _bindings[i].CaptureToSave(_cache);

            var result = await App.Save.SaveAsync(
                slotId,
                _cache,
                SettingsSaveData.TypeId,
                token);

            if (result.Success)
                _dirty = false;

            if (log)
                Debug.Log($"[SettingsSave] success={result.Success} status={result.Status} slot={slotId}");
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
