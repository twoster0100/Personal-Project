using System;

namespace MyGame.Domain.Progress
{
    [Flags]
    public enum ProgressChangedFlags
    {
        None = 0,
        StageIndex = 1 << 0,
        Gold = 1 << 1,
        Gem = 1 << 2,
    }

    public readonly struct PlayerProgressSnapshot
    {
        public readonly int StageIndex;
        public readonly long Gold;
        public readonly long Gem;

        public PlayerProgressSnapshot(int stageIndex, long gold, long gem)
        {
            StageIndex = stageIndex;
            Gold = gold;
            Gem = gem;
        }
    }

    public readonly struct PlayerProgressChanged
    {
        public readonly PlayerProgressSnapshot Before;
        public readonly PlayerProgressSnapshot After;
        public readonly ProgressChangedFlags Flags;
        public readonly string Reason;

        public PlayerProgressChanged(PlayerProgressSnapshot before, PlayerProgressSnapshot after, ProgressChangedFlags flags, string reason)
        {
            Before = before;
            After = after;
            Flags = flags;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>
    ///  골드/젬/스테이지 진행 모델 (UnityEngine 의존 없음)
    /// - 값 변경 시 이벤트 발행
    /// - 값 정규화(스테이지>=1, 골드/젬>=0)
    /// </summary>
    public sealed class PlayerProgressModel
    {
        public event Action<PlayerProgressChanged> Changed;

        private int _stageIndex = 1;
        private long _gold = 0;
        private long _gem = 0;

        public int StageIndex => _stageIndex;
        public long Gold => _gold;
        public long Gem => _gem;

        public PlayerProgressSnapshot Snapshot => new(_stageIndex, _gold, _gem);

        public void ReplaceAll(int stageIndex, long gold, long gem, string reason = "ReplaceAll")
        {
            stageIndex = Math.Max(1, stageIndex);
            gold = Math.Max(0, gold);
            gem = Math.Max(0, gem);

            var before = Snapshot;

            bool stageChanged = stageIndex != _stageIndex;
            bool goldChanged = gold != _gold;
            bool gemChanged = gem != _gem;

            if (!stageChanged && !goldChanged && !gemChanged) return;

            _stageIndex = stageIndex;
            _gold = gold;
            _gem = gem;

            var flags = ProgressChangedFlags.None;
            if (stageChanged) flags |= ProgressChangedFlags.StageIndex;
            if (goldChanged) flags |= ProgressChangedFlags.Gold;
            if (gemChanged) flags |= ProgressChangedFlags.Gem;

            Changed?.Invoke(new PlayerProgressChanged(before, Snapshot, flags, reason));
        }

        public void AddGold(long delta, string reason = "AddGold")
        {
            if (delta == 0) return;

            var before = Snapshot;

            long next = _gold + delta;
            if (next < 0) next = 0;

            if (next == _gold) return;

            _gold = next;
            Changed?.Invoke(new PlayerProgressChanged(before, Snapshot, ProgressChangedFlags.Gold, reason));
        }

        public void AddGem(long delta, string reason = "AddGem")
        {
            if (delta == 0) return;

            var before = Snapshot;

            long next = _gem + delta;
            if (next < 0) next = 0;

            if (next == _gem) return;

            _gem = next;
            Changed?.Invoke(new PlayerProgressChanged(before, Snapshot, ProgressChangedFlags.Gem, reason));
        }

        public void SetStageIndex(int stageIndex, string reason = "SetStageIndex")
        {
            ReplaceAll(stageIndex, _gold, _gem, reason);
        }

        public void AdvanceStage(int delta = 1, string reason = "AdvanceStage")
        {
            if (delta == 0) return;
            ReplaceAll(_stageIndex + delta, _gold, _gem, reason);
        }
    }
}
