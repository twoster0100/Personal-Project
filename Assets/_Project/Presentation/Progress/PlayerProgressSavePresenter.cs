using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MyGame.Application;      // App.Save
using MyGame.Application.Save; // PlayerProgressSaveData

namespace MyGame.Presentation.Progress
{
    /// <summary>
    /// ✅ "진행 데이터 저장" 공통 Presenter (SettingsSavePresenter와 동일 구조)
    /// - Boot가 로드한 슬롯ID를 SetSlotId로 주입ㅐ
    /// - 변경 발생 시 NotifyChangedFromGame()만 호출하면 된다.
    /// - Debounce + Pause/Quit/Disable 플러시(보험)
    /// </summary>
    public sealed class PlayerProgressSavePresenter : MonoBehaviour
    {
        [Header("Save Slot (Boot가 런타임에 SetSlotId로 주입)")]
        [SerializeField] private string slotId = "player/UNKNOWN/progress_0";

        [Header("Bindings Root (비워두면 자기 자신부터 탐색)")]
        [SerializeField] private Transform bindingsRoot;

        [Header("Debounce (seconds)")]
        [SerializeField] private float debounceSeconds = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool log = true;

        private bool _armed;
        private bool _dirty;

        private readonly List<IPlayerProgressBinding> _bindings = new(16);
        private PlayerProgressSaveData _cache = new();

        private CancellationTokenSource _debounceCts;

        public string SlotId => slotId;

        public void SetSlotId(string newSlotId)
        {
            slotId = newSlotId;
            if (log) Debug.Log($"[PlayerSave] SetSlotId = {slotId}");
        }

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
                if (monos[i] is IPlayerProgressBinding b)
                    _bindings.Add(b);
            }

            if (log) Debug.Log($"[PlayerSave] Bindings={_bindings.Count}");
        }

        public void Arm()
        {
            _armed = true;
            if (log) Debug.Log("[PlayerSave] Arm");
        }

        public void Disarm()
        {
            _armed = false;
            _dirty = false;
            CancelDebounce();
            if (log) Debug.Log("[PlayerSave] Disarm");
        }

        public void InitializeCache(PlayerProgressSaveData loadedOrDefault)
        {
            _cache = loadedOrDefault ?? new PlayerProgressSaveData();
        }

        /// <summary>
        /// "진행 데이터가 바뀌었으니 저장해줘!"
        /// - 게임 로직/보상/스테이지 변경에서 이거만 호출하면 됨
        /// </summary>
        public void NotifyChangedFromGame(string reason = null)
        {
            if (!_armed) return;

            _dirty = true;

            if (log)
                Debug.Log($"[PlayerSave] Dirty by GAME. reason={reason ?? "null"}");

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
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }

        private async Task SaveNowAsync(bool force, CancellationToken token)
        {
            if (!_armed) return;
            if (!force && !_dirty) return;

            if (App.Save == null)
            {
                if (log) Debug.LogWarning("[PlayerSave] App.Save is null. Skip.");
                return;
            }

            // 최신 런타임 값을 캐시에 다시 수집(바인딩 기반)
            for (int i = 0; i < _bindings.Count; i++)
                _bindings[i].CaptureToSave(_cache);

            var result = await App.Save.SaveAsync(
                slotId,
                _cache,
                PlayerProgressSaveData.TypeId,
                token);

            if (result.Success)
                _dirty = false;

            if (log)
                Debug.Log($"[PlayerSave] success={result.Success} status={result.Status} slot={slotId}");
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
