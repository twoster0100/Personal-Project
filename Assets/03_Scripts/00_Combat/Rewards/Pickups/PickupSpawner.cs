using UnityEngine;

namespace MyGame.Combat
{
    public sealed class PickupSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private PickupObject expOrbPrefab;
        [SerializeField] private PickupObject itemPickupPrefab;

        [Header("Scatter")]
        [SerializeField] private float scatterRadius = 0.8f;

        private PickupPool _expPool;
        private PickupPool _itemPool;

        private void Awake()
        {
            _expPool = new PickupPool(expOrbPrefab, transform);
            _itemPool = new PickupPool(itemPickupPrefab, transform);
        }

        // -------------------------
        //  (호환) CombatRewardOrchestrator가 호출하는 API
        // -------------------------
        public void SpawnExpOrbs(Vector3 origin, int totalAmount, int splitCount)
        {
            if (totalAmount <= 0) return;

            splitCount = Mathf.Clamp(splitCount, 1, 50); // 과도 분할 방지(임의 상한)
            int baseAmt = totalAmount / splitCount;
            int remainder = totalAmount % splitCount;

            // splitCount가 totalAmount보다 큰 경우 baseAmt=0이 되므로,
            // 최소 1개 이상 유효하게 나오도록 splitCount를 재조정
            if (baseAmt == 0)
            {
                splitCount = Mathf.Clamp(totalAmount, 1, 50);
                baseAmt = totalAmount / splitCount;
                remainder = totalAmount % splitCount;
            }

            for (int i = 0; i < splitCount; i++)
            {
                int amt = baseAmt + (i < remainder ? 1 : 0);
                if (amt <= 0) continue;
                SpawnSingleExp(origin, amt);
            }
        }

        // (선택) 기존 이름으로도 호출할 수 있게 유지
        public void SpawnExp(Vector3 origin, int totalAmount)
        {
            SpawnExpOrbs(origin, totalAmount, splitCount: 1);
        }

        public void SpawnItem(Vector3 origin, string itemId, int amount)
        {
            if (amount <= 0) return;
            SpawnSingleItem(origin, itemId, amount);
        }

        private void SpawnSingleExp(Vector3 origin, int amount)
        {
            var p = _expPool.Get();
            if (p == null) return;

            var target = Scatter(origin);

            p.gameObject.SetActive(true);
            p.SpawnedBy(this, PickupKind.ExpOrb, amount, null, origin, target);
        }

        private void SpawnSingleItem(Vector3 origin, string itemId, int amount)
        {
            var p = _itemPool.Get();
            if (p == null) return;

            var target = Scatter(origin);

            p.gameObject.SetActive(true);
            p.SpawnedBy(this, PickupKind.Item, amount, itemId, origin, target);
        }

        private Vector3 Scatter(Vector3 origin)
        {
            Vector2 rnd = Random.insideUnitCircle * scatterRadius;
            return origin + new Vector3(rnd.x, 0f, rnd.y);
        }

        internal void Release(PickupObject obj)
        {
            if (obj == null) return;

            obj.gameObject.SetActive(false);

            if (obj.Kind == PickupKind.ExpOrb) _expPool.Release(obj);
            else _itemPool.Release(obj);
        }

        // -------------------------
        // 내부 풀 (MVP)
        // -------------------------
        private sealed class PickupPool
        {
            private readonly PickupObject _prefab;
            private readonly Transform _parent;
            private readonly System.Collections.Generic.Stack<PickupObject> _stack = new();

            public PickupPool(PickupObject prefab, Transform parent)
            {
                _prefab = prefab;
                _parent = parent;
            }

            public PickupObject Get()
            {
                if (_prefab == null) return null;

                if (_stack.Count > 0) return _stack.Pop();

                var go = GameObject.Instantiate(_prefab.gameObject, _parent);
                go.SetActive(false);
                return go.GetComponent<PickupObject>();
            }

            public void Release(PickupObject obj)
            {
                if (obj == null) return;
                _stack.Push(obj);
            }
        }
    }
}
