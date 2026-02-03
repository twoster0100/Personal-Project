using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Combat
{
    public sealed class PickupSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private PickupObject expOrbPrefab;
        [SerializeField] private PickupObject itemPrefab;

        [Header("Pooling")]
        [SerializeField] private int prewarmEach = 12;
        [SerializeField] private int maxPoolEach = 128;

        [Header("Spawn Control")]
        [SerializeField] private int maxActive = 48;
        [SerializeField] private float scatterRadius = 0.6f;
        [SerializeField] private bool mergeWhenFull = true;

        private Pool _expPool;
        private Pool _itemPool;

        private readonly HashSet<PickupObject> _active = new();

        private void Awake()
        {
            _expPool = new Pool(expOrbPrefab, prewarmEach, maxPoolEach, transform);
            _itemPool = new Pool(itemPrefab, prewarmEach, maxPoolEach, transform);
        }

        public void SpawnExpOrbs(Vector3 origin, int totalExp, int splitCount = 3)
        {
            totalExp = Mathf.Max(0, totalExp);
            if (totalExp <= 0) return;

            splitCount = Mathf.Clamp(splitCount, 1, 12);

            int allowed = Mathf.Max(0, maxActive - _active.Count);
            if (allowed <= 0)
            {
                if (mergeWhenFull)
                    SpawnSingleExp(origin, totalExp);
                return;
            }

            int actual = Mathf.Min(splitCount, allowed);
            int each = totalExp / actual;
            int rem = totalExp - each * actual;

            for (int i = 0; i < actual; i++)
            {
                int amt = each + (i == 0 ? rem : 0);
                if (amt <= 0) continue;

                SpawnSingleExp(origin, amt);
            }
        }

        public void SpawnItem(Vector3 origin, string itemId, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            count = Mathf.Max(1, count);

            if (_active.Count >= maxActive)
            {
                if (!mergeWhenFull) return;
            }

            SpawnSingleItem(origin, itemId, count);
        }

        internal void Release(PickupObject pickup)
        {
            if (pickup == null) return;

            _active.Remove(pickup);
            pickup.gameObject.SetActive(false);

            if (pickup.Kind == PickupKind.ExpOrb)
                _expPool.Return(pickup);
            else
                _itemPool.Return(pickup);
        }

        private void SpawnSingleExp(Vector3 origin, int amount)
        {
            var p = _expPool.Get();
            if (p == null) return;

            p.transform.position = Scatter(origin);
            p.SpawnedBy(this, PickupKind.ExpOrb, amount, null);
            p.gameObject.SetActive(true);

            _active.Add(p);
        }

        private void SpawnSingleItem(Vector3 origin, string itemId, int amount)
        {
            var p = _itemPool.Get();
            if (p == null) return;

            p.transform.position = Scatter(origin);
            p.SpawnedBy(this, PickupKind.Item, amount, itemId);
            p.gameObject.SetActive(true);

            _active.Add(p);
        }

        private Vector3 Scatter(Vector3 origin)
        {
            if (scatterRadius <= 0f) return origin;
            var offset = Random.insideUnitSphere;
            offset.y = 0f;
            return origin + offset.normalized * Random.Range(0f, scatterRadius);
        }

        private sealed class Pool
        {
            private readonly PickupObject _prefab;
            private readonly Transform _parent;
            private readonly int _max;
            private readonly Stack<PickupObject> _stack = new();
            private int _created;

            public Pool(PickupObject prefab, int prewarm, int max, Transform parent)
            {
                _prefab = prefab;
                _parent = parent;
                _max = Mathf.Max(0, max);

                if (_prefab == null) return;

                prewarm = Mathf.Max(0, prewarm);
                for (int i = 0; i < prewarm; i++)
                {
                    var inst = CreateNew();
                    if (inst == null) break;
                    inst.gameObject.SetActive(false);
                    _stack.Push(inst);
                }
            }

            public PickupObject Get()
            {
                if (_prefab == null) return null;

                while (_stack.Count > 0)
                {
                    var p = _stack.Pop();
                    if (p != null) return p;
                }

                return CreateNew();
            }

            public void Return(PickupObject p)
            {
                if (p == null) return;
                _stack.Push(p);
            }

            private PickupObject CreateNew()
            {
                if (_prefab == null) return null;
                if (_max > 0 && _created >= _max) return null;

                var inst = Object.Instantiate(_prefab, _parent);
                _created++;
                return inst;
            }
        }
    }
}
