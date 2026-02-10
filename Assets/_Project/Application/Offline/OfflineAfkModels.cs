using System;

namespace MyGame.Application.Offline
{
    public interface IOfflineAfkBalanceSource
    {
        OfflineAfkCell ResolveCell(int stageIndex, int powerTier);
    }

    [Serializable]
    public struct OfflineAfkRule
    {
        public int maxHoursCap;
    }

    [Serializable]
    public struct OfflineAfkCell
    {
        public long goldPerSecond;
        public long expPerSecond;
        public double dropPerSecond;
    }

    [Serializable]
    public struct OfflineAfkInput
    {
        public long elapsedSeconds;
        public int stageIndex;
        public int powerTier;
        public double dropCarry;
    }

    [Serializable]
    public struct OfflineAfkResult
    {
        public long cappedSeconds;
        public int cappedHours;
        public long gold;
        public long exp;
        public long dropCount;
        public double nextDropCarry;

        public bool HasReward => gold > 0 || exp > 0 || dropCount > 0;
    }
}
