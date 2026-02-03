using System;
using System.Collections.Generic;

namespace MyGame.Domain.Rewards
{
    public enum RewardKind { Gold, Exp, Item }

    public readonly struct Reward
    {
        public readonly RewardKind Kind;
        public readonly long Amount;
        public readonly string ItemId;

        public Reward(RewardKind kind, long amount, string itemId = null)
        {
            Kind = kind;
            Amount = amount;
            ItemId = itemId ?? string.Empty;
        }

        public override string ToString()
        {
            return Kind switch
            {
                RewardKind.Item => $"Item({ItemId}) x{Amount}",
                _ => $"{Kind} +{Amount}"
            };
        }
    }

    public sealed class RewardBundle
    {
        public readonly List<Reward> Rewards = new();
        public void Add(Reward reward)
        {
            if (reward.Amount <= 0) return;
            Rewards.Add(reward);
        }
    }

    public interface IRng
    {
        double Next01();
        int RangeInt(int minInclusive, int maxInclusive);
    }

    public sealed class SystemRng : IRng
    {
        private readonly Random _r;
        public SystemRng(int? seed = null) => _r = seed.HasValue ? new Random(seed.Value) : new Random();
        public double Next01() => _r.NextDouble();
        public int RangeInt(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive) (minInclusive, maxInclusive) = (maxInclusive, minInclusive);
            return _r.Next(minInclusive, maxInclusive + 1);
        }
    }

    public sealed class DropTable
    {
        public float GoldEvMin = 0f;
        public float GoldEvMax = 0f;
        public int ExpMin = 0;
        public int ExpMax = 0;
        public readonly List<ItemDropEntry> Items = new();
    }

    public sealed class ItemDropEntry
    {
        public string ItemId = string.Empty;
        public float Chance01 = 0f;
        public int CountMin = 1;
        public int CountMax = 1;
    }

    public static class DropResolver
    {
        public static RewardBundle Resolve(DropTable table, IRng rng)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var bundle = new RewardBundle();

            float ev = SampleRange(table.GoldEvMin, table.GoldEvMax, rng);
            long gold = SampleIntegerFromEV(ev, rng);
            if (gold > 0) bundle.Add(new Reward(RewardKind.Gold, gold));

            int exp = SampleIntRange(table.ExpMin, table.ExpMax, rng);
            if (exp > 0) bundle.Add(new Reward(RewardKind.Exp, exp));

            for (int i = 0; i < table.Items.Count; i++)
            {
                var it = table.Items[i];
                if (it == null) continue;

                float chance = Clamp01(it.Chance01);
                if (chance <= 0f) continue;

                if (rng.Next01() < chance)
                {
                    int cnt = SampleIntRange(it.CountMin, it.CountMax, rng);
                    if (cnt > 0)
                        bundle.Add(new Reward(RewardKind.Item, cnt, it.ItemId));
                }
            }

            return bundle;
        }

        private static float SampleRange(float min, float max, IRng rng)
        {
            if (max < min) (min, max) = (max, min);
            double t = rng.Next01();
            return (float)(min + (max - min) * t);
        }

        private static int SampleIntRange(int min, int max, IRng rng)
        {
            if (max < min) (min, max) = (max, min);
            return rng.RangeInt(min, max);
        }

        private static long SampleIntegerFromEV(float expected, IRng rng)
        {
            if (expected <= 0f) return 0;

            double baseVal = Math.Floor(expected);
            double frac = expected - baseVal;

            long v = (long)baseVal;
            if (frac > 0.0 && rng.Next01() < frac) v += 1;

            return v;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
