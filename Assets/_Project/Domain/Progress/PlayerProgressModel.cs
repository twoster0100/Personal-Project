using System;

namespace MyGame.Domain.Progress
{
    [Flags]
    public enum ProgressChangedFlags { None = 0, StageIndex = 1 << 0, Gold = 1 << 1 }

    public readonly struct PlayerProgressSnapshot
    {
        public readonly int StageIndex;
        public readonly long Gold;
        public PlayerProgressSnapshot(int stageIndex, long gold) { StageIndex = stageIndex; Gold = gold; }
    }

    public readonly struct PlayerProgressChanged
    {
        public readonly PlayerProgressSnapshot Before;
        public readonly PlayerProgressSnapshot After;
        public readonly ProgressChangedFlags Flags;
        public readonly string Reason;

        public PlayerProgressChanged(PlayerProgressSnapshot before, PlayerProgressSnapshot after, ProgressChangedFlags flags, string reason)
        {
            Before = before; After = after; Flags = flags; Reason = reason ?? string.Empty;
        }
    }

    public sealed class PlayerProgressModel
    {
        public event Action<PlayerProgressChanged> Changed;

        private int _stageIndex = 1;
        private long _gold = 0;

        public int StageIndex => _stageIndex;
        public long Gold => _gold;
        public PlayerProgressSnapshot Snapshot => new(_stageIndex, _gold);

        public void ReplaceAll(int stageIndex, long gold, string reason = "ReplaceAll")
        {
            stageIndex = Math.Max(1, stageIndex);
            gold = Math.Max(0, gold);

            var before = Snapshot;

            bool stageChanged = stageIndex != _stageIndex;
            bool goldChanged = gold != _gold;
            if (!stageChanged && !goldChanged) return;

            _stageIndex = stageIndex;
            _gold = gold;

            var flags = ProgressChangedFlags.None;
            if (stageChanged) flags |= ProgressChangedFlags.StageIndex;
            if (goldChanged) flags |= ProgressChangedFlags.Gold;

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

        public void SetStageIndex(int stageIndex, string reason = "SetStageIndex")
            => ReplaceAll(stageIndex, _gold, reason);

        public void AdvanceStage(int delta = 1, string reason = "AdvanceStage")
        {
            if (delta == 0) return;
            ReplaceAll(_stageIndex + delta, _gold, reason);
        }
    }
}
