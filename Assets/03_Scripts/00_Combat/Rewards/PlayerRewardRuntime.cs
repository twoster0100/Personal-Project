using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// MVP 단계에서 "픽업으로 들어온 EXP/아이템"을 런타임에 누적/검증하기 위한 임시 보상 수신기.
    /// - 아직 인벤토리/레벨업 시스템이 없어도, 픽업 루프를 검증할 수 있게 만든다.
    /// - 추후 Inventory/Exp 시스템이 생기면 이 컴포넌트는 Adapter/Facade 역할로 교체/흡수 가능.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerRewardRuntime : MonoBehaviour
    {
        [Header("Debug Snapshot (Inspector)")]
        [SerializeField, Min(0)] private int exp = 0;

        [SerializeField] private List<ItemStack> items = new();

        [Serializable]
        public struct ItemStack
        {
            public string itemId;
            public int amount;
        }

        public int Exp => exp;
        public IReadOnlyList<ItemStack> Items => items;

        public event Action<int> ExpChanged;
        public event Action<string, int> ItemAddedOrIncreased;

        /// <summary>EXP를 더한다(음수 입력은 무시).</summary>
        public void AddExp(int delta)
        {
            if (delta <= 0) return;

            exp += delta;
            ExpChanged?.Invoke(exp);

           // Debug.Log($"[RewardRuntime] EXP +{delta} => {exp}");
        }

        /// <summary>아이템을 더한다(잘못된 id/수량이면 무시). 같은 itemId는 합쳐서 누적.</summary>
        public void AddItem(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            if (amount <= 0) return;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].itemId == itemId)
                {
                    var s = items[i];
                    s.amount += amount;
                    items[i] = s;

                    ItemAddedOrIncreased?.Invoke(itemId, s.amount);
                    Debug.Log($"[RewardRuntime] Item({itemId}) +{amount} => {s.amount}");
                    return;
                }
            }

            items.Add(new ItemStack { itemId = itemId, amount = amount });
            ItemAddedOrIncreased?.Invoke(itemId, amount);

            Debug.Log($"[RewardRuntime] Item({itemId}) x{amount} (new)");
        }

        [ContextMenu("Debug/Clear Rewards")]
        private void DebugClear()
        {
            exp = 0;
            items.Clear();
            ExpChanged?.Invoke(exp);
            Debug.Log("[RewardRuntime] Cleared");
        }
    }
}
