using UnityEngine;

namespace MyGame.Combat
{
    public enum PickupKind { ExpOrb, Item }

    [DisallowMultipleComponent]
    public sealed class PickupObject : MonoBehaviour
    {
        [Header("Payload (readonly at runtime)")]
        [SerializeField] private PickupKind kind;
        [SerializeField] private string itemId;
        [SerializeField] private int amount;

        private PickupSpawner _owner;
        private bool _collected;

        public PickupKind Kind => kind;
        public string ItemId => itemId;
        public int Amount => amount;

        internal void SpawnedBy(PickupSpawner owner, PickupKind k, int amt, string id)
        {
            _owner = owner;
            kind = k;
            amount = Mathf.Max(0, amt);
            itemId = id ?? string.Empty;
            _collected = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;

            var collector = other.GetComponentInParent<PickupCollector>();
            if (collector == null) return;

            if (collector.TryCollect(this))
            {
                _collected = true;
                if (_owner != null) _owner.Release(this);
                else gameObject.SetActive(false);
            }
        }
    }
}
