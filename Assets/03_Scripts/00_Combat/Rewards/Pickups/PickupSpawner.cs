using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Combat
{
    [DisallowMultipleComponent]
    public sealed class PickupSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private PickupObject expOrbPrefab;
        [SerializeField] private PickupObject itemPickupPrefab;

        [Header("Drop Scatter")]
        [SerializeField] private float scatterRadius = 1.2f;
        [SerializeField] private float originYOffset = 0.2f;

        [Header("Item rarity (optional override)")]
        [SerializeField] private List<ItemRarityOverride> rarityOverrides = new();

        [Serializable]
        private struct ItemRarityOverride
        {
            public string itemId;
            public PickupRarity rarity;
        }

        public void SpawnExpOrbs(Vector3 worldPos, int totalExp, int splitCount)
        {
            if (expOrbPrefab == null) return;
            if (totalExp <= 0) return;

            splitCount = Mathf.Max(1, splitCount);
            splitCount = Mathf.Min(splitCount, totalExp);

            int baseAmount = totalExp / splitCount;
            int rem = totalExp % splitCount;

            for (int i = 0; i < splitCount; i++)
            {
                int amt = baseAmount + (i < rem ? 1 : 0);
                if (amt <= 0) continue;

                var po = Instantiate(expOrbPrefab, transform);
                po.SetupExp(amt);

                MakeScatter(worldPos, out var origin, out var landing);
                po.BeginDrop(origin, landing);
            }
        }

        // 하위호환/편의
        public void SpawnExpOrb(Vector3 worldPos, int expAmount) => SpawnExpOrbs(worldPos, expAmount, splitCount: 1);
        public void SpawnExp(Vector3 worldPos, int expAmount) => SpawnExpOrbs(worldPos, expAmount, splitCount: 1);

        public void SpawnItem(Vector3 worldPos, string itemId, int amount)
        {
            SpawnItem(worldPos, itemId, amount, ResolveRarity(itemId));
        }

        public void SpawnItem(Vector3 worldPos, string itemId, int amount, PickupRarity rarity)
        {
            if (itemPickupPrefab == null) return;
            if (string.IsNullOrWhiteSpace(itemId)) return;
            if (amount <= 0) return;

            var po = Instantiate(itemPickupPrefab, transform);
            po.SetupItem(itemId, amount, rarity);

            MakeScatter(worldPos, out var origin, out var landing);
            po.BeginDrop(origin, landing);
        }

        private void MakeScatter(Vector3 worldPos, out Vector3 origin, out Vector3 landing)
        {
            origin = worldPos + Vector3.up * originYOffset;

            Vector2 r = UnityEngine.Random.insideUnitCircle * scatterRadius;
            landing = worldPos + new Vector3(r.x, 0f, r.y);
        }

        private PickupRarity ResolveRarity(string id)
        {
            if (string.IsNullOrEmpty(id)) return PickupRarity.Common;

            for (int i = 0; i < rarityOverrides.Count; i++)
            {
                if (rarityOverrides[i].itemId == id)
                    return rarityOverrides[i].rarity;
            }

            return PickupRarity.Common;
        }
    }
}
