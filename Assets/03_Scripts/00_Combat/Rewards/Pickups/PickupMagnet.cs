using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// 플레이어(또는 파티 리더)에 붙이는 픽업 자석.
    /// - 주기적으로 주변 Pickup 레이어를 스캔
    /// - PickupObject.TryStartMagnet() 호출
    ///
    ///  BF(습득거리) 스케일링:
    /// radius = baseRadius + radiusPerBF * BF
    /// 여기서 BF는 "ActorStats의 BF 레벨(투자/성장 합)"을 사용한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PickupMagnet : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Actor owner;
        [SerializeField] private PickupCollector collector;
        [SerializeField] private Transform magnetTarget;

        [Header("Range (BF scaling)")]
        [SerializeField] private float baseRadius = 2.0f;

        [Tooltip("BF 1당 증가량. (현재 구현은 'BF 레벨' 기준)")]
        [SerializeField] private float radiusPerBF = 0.01f;

        [Header("Scan")]
        [SerializeField] private LayerMask pickupLayerMask;
        [SerializeField] private float scanInterval = 0.1f;

        [Tooltip("동시에 감지할 픽업 수(NonAlloc 버퍼). 화면에 동시에 떨어질 수 있는 최대치로 잡기")]
        [SerializeField] private int overlapBufferSize = 32;

        private float _nextScanUnscaled;
        private Collider[] _buffer;

        private void Reset()
        {
            if (owner == null) owner = GetComponent<Actor>();
            if (collector == null) collector = GetComponent<PickupCollector>();
            if (magnetTarget == null) magnetTarget = transform;
        }

        private void Awake()
        {
            if (owner == null) owner = GetComponent<Actor>();
            if (collector == null) collector = GetComponent<PickupCollector>();
            if (magnetTarget == null) magnetTarget = transform;

            overlapBufferSize = Mathf.Clamp(overlapBufferSize, 8, 256);
            _buffer = new Collider[overlapBufferSize];
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (now < _nextScanUnscaled) return;
            _nextScanUnscaled = now + Mathf.Max(0.01f, scanInterval);

            ScanOnce();
        }

        private void ScanOnce()
        {
            if (collector == null) return;
            if (magnetTarget == null) return;

            float radius = ComputeRadius();
            Vector3 center = transform.position;

            int hitCount = Physics.OverlapSphereNonAlloc(center, radius, _buffer, pickupLayerMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                var col = _buffer[i];
                if (col == null) continue;

                var po = col.GetComponentInParent<PickupObject>();
                if (po == null) continue;

                po.TryStartMagnet(magnetTarget, collector);
            }
        }

        private float ComputeRadius()
        {
            float bf = 0f;

            if (owner != null && owner.Stats != null)
            {
                // ✅ BF는 "레벨 합" 기반으로 사용 (투자/성장에 즉각 반응)
                bf = owner.Stats.GetTotalStatLevel(StatId.BF);
            }
            else if (owner != null)
            {
                bf = owner.GetFinalStat(StatId.BF);
            }

            float r = baseRadius + radiusPerBF * Mathf.Max(0f, bf);
            return Mathf.Max(0.1f, r);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float r = baseRadius;
            if (owner != null && owner.Stats != null)
                r = baseRadius + radiusPerBF * Mathf.Max(0f, owner.Stats.GetTotalStatLevel(StatId.BF));

            Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, r));
        }
#endif
    }
}
