using System;
using UnityEngine;
using MyGame.Application.Save;
using MyGame.Domain.Progress;

namespace MyGame.Presentation.Progress
{
    /// <summary>
    /// ✅ 런타임 진행 데이터 바인딩(골드/젬/스테이지)
    /// - Domain: PlayerProgressModel(순수 로직) 보유
    /// - Presentation: HUD 갱신/저장 트리거용 이벤트 발행
    /// </summary>
    public sealed class PlayerProgressRuntimeBinding : MonoBehaviour, IPlayerProgressBinding
    {
        [Header("Debug Mirror (Inspector Only)")]
        [SerializeField] private int stageIndex = 1;
        [SerializeField] private long gold = 0;
        [SerializeField] private long gem = 0;

        public int StageIndex => _model.StageIndex;
        public long Gold => _model.Gold;
        public long Gem => _model.Gem;

        public PlayerProgressModel Model => _model;

        public event Action<PlayerProgressChanged> ProgressChanged;

        private readonly PlayerProgressModel _model = new();

        [Header("Optional Wiring")]
        [SerializeField] private PlayerProgressSavePresenter savePresenter;

        private void Awake()
        {
            _model.Changed += OnModelChanged;

            stageIndex = _model.StageIndex;
            gold = _model.Gold;
            gem = _model.Gem;
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
            gem = e.After.Gem;

            ProgressChanged?.Invoke(e);

            // ✅ 저장 트리거(디바운스)
            if (savePresenter != null)
                savePresenter.NotifyChangedFromGame(reason: e.Reason);
        }

        // =============================
        // IPlayerProgressBinding
        // =============================
        public void ApplyFromSave(PlayerProgressSaveData data)
        {
            _model.ReplaceAll(data.stageIndex, data.gold, data.gem, reason: "ApplyFromSave");
        }

        public void CaptureToSave(PlayerProgressSaveData data)
        {
            var snap = _model.Snapshot;
            data.stageIndex = snap.StageIndex;
            data.gold = snap.Gold;
            data.gem = snap.Gem;
        }

        // =============================
        // Gameplay API
        // =============================
        public void AddGold(long amount, string reason = "AddGold")
        {
            if (amount == 0) return;
            _model.AddGold(amount, reason);
        }

        public void AddGem(long amount, string reason = "AddGem")
        {
            if (amount == 0) return;
            _model.AddGem(amount, reason);
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
        // Debug
        // =============================
        [ContextMenu("Debug/Add 100 Gold")]
        private void DebugAddGold()
        {
            AddGold(100, "DebugAddGold");
        }

        [ContextMenu("Debug/Add 10 Gem")]
        private void DebugAddGem()
        {
            AddGem(10, "DebugAddGem");
        }

        [ContextMenu("Debug/Next Stage")]
        private void DebugNextStage()
        {
            AdvanceStage(1, "DebugNextStage");
        }
    }
}
