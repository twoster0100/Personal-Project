using UnityEngine;
using MyGame.Application;
using MyGame.Application.Tick;

namespace MyGame.Combat
{
    /// <summary>
    /// ✅ 트리거 의존을 줄이기 위한 '자석 수집' MVP.
    /// - 일정 주기마다 OverlapSphereNonAlloc로 근처 픽업을 찾고
    /// - PickupObject.TryBeginMagnet(...)을 호출해 DOTween으로 빨려들게 만든다.
    /// - 수집 확정은 PickupObject가 도착 시점에 collector.TryCollect(...)를 호출한다.
    /// </summary>
    public sealed class PickupMagnet : MonoBehaviour, ISimulationTickable
    {
        [Header("Wiring")]
        [SerializeField] private Actor owner;
        [SerializeField] private PickupCollector collector;
        [Tooltip("자석이 빨려들어갈 목표(보통 플레이어의 가슴/루트). 비워두면 transform 사용")]
        [SerializeField] private Transform magnetTarget;

        [Header("Range (BF scaling)")]
        [SerializeField] private float baseRadius = 1.8f;
        [SerializeField] private float radiusPerBF = 0.25f;

        [Header("Scan")]
        [SerializeField] private LayerMask pickupLayerMask = ~0;
        [SerializeField] private float scanInterval = 0.05f;
        [SerializeField] private int overlapBufferSize = 32;

        private float _scanRemain;
        private Collider[] _buf;

        private void Reset()
        {
            owner = GetComponent<Actor>();
            collector = GetComponentInChildren<PickupCollector>();
            magnetTarget = transform;
        }

        private void Awake()
        {
            if (owner == null) owner = GetComponent<Actor>();
            if (collector == null) collector = GetComponentInChildren<PickupCollector>();
            if (magnetTarget == null) magnetTarget = transform;

            overlapBufferSize = Mathf.Clamp(overlapBufferSize, 8, 256);
            _buf = new Collider[overlapBufferSize];
        }

        private void OnEnable()
        {
            App.RegisterWhenReady(this);
        }

        private void OnDisable()
        {
            App.UnregisterTickable(this);
        }

        public void SimulationTick(float dt)
        {
            if (owner == null || collector == null) return;
            if (!owner.IsAlive) return;

            _scanRemain -= dt;
            if (_scanRemain > 0f) return;
            _scanRemain = Mathf.Max(0.01f, scanInterval);

            float radius = ComputeRadius();

            int hit = Physics.OverlapSphereNonAlloc(
                owner.transform.position,
                radius,
                _buf,
                pickupLayerMask,
                QueryTriggerInteraction.Collide);

            if (hit <= 0) return;

            Transform target = (magnetTarget != null) ? magnetTarget : owner.transform;

            for (int i = 0; i < hit; i++)
            {
                var col = _buf[i];
                if (col == null) continue;

                var pickup = col.GetComponentInParent<PickupObject>();
                if (pickup == null) continue;

                pickup.TryBeginMagnet(collector, target);
            }
        }

        private float ComputeRadius()
        {
            // BF가 float이므로 안전하게 int로 반올림/내림 선택 가능
            int bf = Mathf.Max(0, Mathf.FloorToInt(owner.GetFinalStat(StatId.BF)));
            return baseRadius + bf * radiusPerBF;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (owner == null) owner = GetComponent<Actor>();
            if (owner == null) return;

            float radius = baseRadius;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(owner.transform.position, radius);
        }
#endif
    }
}
