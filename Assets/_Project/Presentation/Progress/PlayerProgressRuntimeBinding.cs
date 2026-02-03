using System;
using UnityEngine;
using MyGame.Application.Save;
using MyGame.Domain.Progress;

namespace MyGame.Presentation.Progress
{
    /// <summary>
    /// 런타임 진행 데이터 바인딩(골드/스테이지)
    /// - Domain: PlayerProgressModel(순수 로직) 보유
    /// - Presentation: HUD 갱신/저장 트리거용 이벤트 발행
    /// </summary>
    public sealed class PlayerProgressRuntimeBinding : MonoBehaviour, IPlayerProgressBinding
    {
        [Header("Debug Mirror (Inspector Only)")]
        [SerializeField] private int stageIndex = 1;
        [SerializeField] private long gold = 0;

        public int StageIndex => _model.StageIndex;
        public long Gold => _model.Gold;

        public PlayerProgressModel Model => _model;

        /// <summary> 진행값 변경 이벤트(원인 포함)</summary>
        public event Action<PlayerProgressChanged> ProgressChanged;

        private readonly PlayerProgressModel _model = new();

        [Header("Optional Wiring")]
        [SerializeField] private PlayerProgressSavePresenter savePresenter;

        private void Awake()
        {
            _model.Changed += OnModelChanged;

            stageIndex = _model.StageIndex;
            gold = _model.Gold;
        }

        private void Reset()
        {
            if (savePresenter == null)
                savePresenter = GetComponent<PlayerProgressSavePresenter>();
        }

        private void OnDestroy()
        {
            _model.Changed -= OnModelChanged;
        }

        private void OnModelChanged(PlayerProgressChanged e)
        {
            stageIndex = e.After.StageIndex;
            gold = e.After.Gold;

            ProgressChanged?.Invoke(e);

            // 저장 요청(디바운스). 로드 중에는 SavePresenter가 Disarm되어있으므로 저장되지 않음.
            if (savePresenter != null)
                savePresenter.NotifyChangedFromGame(e.Reason);
        }

        // =============================
        // IPlayerProgressBinding
        // =============================
        public void ApplyFromSave(PlayerProgressSaveData data)
        {
            _model.ReplaceAll(data.stageIndex, data.gold, reason: "ApplyFromSave");
        }

        public void CaptureToSave(PlayerProgressSaveData data)
        {
            var snap = _model.Snapshot;
            data.stageIndex = snap.StageIndex;
            data.gold = snap.Gold;
        }

        // =============================
        // Gameplay API
        // =============================
        public void AddGold(long amount, string reason = "AddGold")
        {
            if (amount == 0) return;
            _model.AddGold(amount, reason);
        }

        public void SetStageIndex(int stage, string reason = "SetStageIndex")
        {
            _model.SetStageIndex(stage, reason);
        }

        public void AdvanceStage(int delta = 1, string reason = "AdvanceStage")
        {
            _model.AdvanceStage(delta, reason);
        }

        // =============================
        // Debug Buttons
        // =============================
        [ContextMenu("Debug/Add 100 Gold")]
        private void DebugAddGold()
        {
            AddGold(100, "DebugAddGold");
        }

        [ContextMenu("Debug/Next Stage")]
        private void DebugNextStage()
        {
            AdvanceStage(1, "DebugNextStage");
        }
    }
}
