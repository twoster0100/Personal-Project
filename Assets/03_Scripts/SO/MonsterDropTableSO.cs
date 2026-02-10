using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Domain.Rewards;

namespace MyGame.Combat
{
    [CreateAssetMenu(menuName = "Combat/Drop Table", fileName = "DT_")]
    public sealed class MonsterDropTableSO : ScriptableObject
    {
        [Header("Gold EV (Expected Value)")]
        public float goldEvMin = 0f;
        public float goldEvMax = 0f;

        [Header("Gem EV (Expected Value)")]
        public float gemEvMin = 0f;
        public float gemEvMax = 0f;

        [Header("Exp (pickup amount)")]
        [Range(0f, 1f)] public float expChance01 = 1f; // ✅ 추가: Exp 드랍 확률(0~1)
        public int expMin = 0;
        public int expMax = 0;

        [Header("Items (independent chance)")]
        public List<ItemEntry> items = new();

        [Serializable]
        public sealed class ItemEntry
        {
            public string itemId = "Item";
            [Range(0f, 1f)] public float chance01 = 0f;
            public int countMin = 1;
            public int countMax = 1;
        }

        public DropTable ToDomain()
        {
            var t = new DropTable
            {
                GoldEvMin = goldEvMin,
                GoldEvMax = goldEvMax,
                GemEvMin = gemEvMin,
                GemEvMax = gemEvMax,

                ExpChance01 = expChance01,
                ExpMin = expMin,
                ExpMax = expMax
            };

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var e = items[i];
                    if (e == null) continue;

                    var entry = new ItemDropEntry
                    {
                        ItemId = e.itemId ?? string.Empty,
                        Chance01 = e.chance01,
                        CountMin = e.countMin,
                        CountMax = e.countMax
                    };

                    t.Items.Add(entry);
                }
            }

            return t;
        }
    }
}
