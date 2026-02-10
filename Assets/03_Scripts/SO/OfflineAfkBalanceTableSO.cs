using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Application.Offline;

namespace MyGame.Presentation.Combat
{
    [CreateAssetMenu(menuName = "Combat/Offline AFK Balance Table", fileName = "OfflineAfkBalanceTable")]
    public sealed class OfflineAfkBalanceTableSO : ScriptableObject, IOfflineAfkBalanceSource
    {
        [Serializable]
        private struct CellEntry
        {
            public int stageIndex;
            public int powerTier;
            public long goldPerSecond;
            public long expPerSecond;
            public float dropPerSecond;
        }

        [SerializeField] private List<CellEntry> cells = new();

        public OfflineAfkCell ResolveCell(int stageIndex, int powerTier)
        {
            stageIndex = Mathf.Max(1, stageIndex);
            powerTier = Mathf.Max(0, powerTier);

            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (c.stageIndex == stageIndex && c.powerTier == powerTier)
                {
                    return new OfflineAfkCell
                    {
                        goldPerSecond = Math.Max(0L, c.goldPerSecond),
                        expPerSecond = Math.Max(0L, c.expPerSecond),
                        dropPerSecond = Math.Max(0f, c.dropPerSecond)
                    };
                }
            }

            return default;
        }
    }
}
