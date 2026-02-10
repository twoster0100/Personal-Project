using System;

namespace MyGame.Application.Offline
{
    public static class OfflineAfkCalculator
    {
        public static OfflineAfkResult Compute(
            in OfflineAfkRule rule,
            in OfflineAfkInput input,
            Func<int, int, OfflineAfkCell> resolveCell)
        {
            int hourCap = Math.Max(1, rule.maxHoursCap);
            long maxSeconds = hourCap * 3600L;

            long cappedSeconds = Math.Max(0L, Math.Min(input.elapsedSeconds, maxSeconds));
            if (cappedSeconds <= 0)
            {
                return new OfflineAfkResult
                {
                    cappedSeconds = 0,
                    cappedHours = hourCap,
                    gold = 0,
                    exp = 0,
                    dropCount = 0,
                    nextDropCarry = Math.Max(0d, input.dropCarry)
                };
            }

            OfflineAfkCell cell = resolveCell != null
                ? resolveCell(input.stageIndex, input.powerTier)
                : default;

            long gold = cell.goldPerSecond * cappedSeconds;
            long exp = cell.expPerSecond * cappedSeconds;

            double rawDrop = Math.Max(0d, input.dropCarry) + Math.Max(0d, cell.dropPerSecond) * cappedSeconds;
            long dropCount = (long)Math.Floor(rawDrop);
            double nextCarry = rawDrop - dropCount;

            return new OfflineAfkResult
            {
                cappedSeconds = cappedSeconds,
                cappedHours = hourCap,
                gold = Math.Max(0L, gold),
                exp = Math.Max(0L, exp),
                dropCount = Math.Max(0L, dropCount),
                nextDropCarry = Math.Max(0d, nextCarry)
            };
        }
    }
}
