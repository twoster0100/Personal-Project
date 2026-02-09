using UnityEngine;
using MyGame.Application;
using MyGame.Application.Tick;

namespace MyGame.Combat
{
    /// <summary>
    /// 플레이어(또는 파티 리더)에 붙이는 픽업 자석.
    /// - 주기적으로 주변 Pickup 레이어를 스캔
    /// - PickupObject.TryStartMagnet() 호출
    ///
    /// 반경 스케일링(권장):
    /// - StatScaledFloatStrategySO 로 분리
    /// - valueSource = FinalWithStatus (버프/디버프가 흡입 반경에도 반영)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PickupMagnet : MonoBehaviour, IUnscaledFrameTickable
    {
        [Header("Wiring")]
        [SerializeField] private Actor owner;
        [SerializeField] private PickupCollector collector;
        [SerializeField] private Transform magnetTarget;

        [Header("Range")]
        [SerializeField] private float baseRadius = 2.0f;

        [Tooltip("반경 스케일링 Strategy (권장: FinalWithStatus 기반)")]
        [SerializeField] private StatScaledFloatStrategySO radiusScaling;

        [Header("Scan")]
        [SerializeField] private LayerMask pickupLayerMask;
        [SerializeField] private float scanInterval = 0.1f;

        [Tooltip("동시에 감지할 픽업 수(NonAlloc 버퍼). 화면에 동시에 떨어질 수 있는 최대치로 잡기")]
        [SerializeField] private int overlapBufferSize = 32;

        private Collider[] _buffer;

        // 남은 시간(언스케일 기준). 0 이하가 되면 ScanOnce 수행.
        private float _scanCooldownUnscaled;

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

            EnsureBuffer();
            _scanCooldownUnscaled = 0f;
        }

        private void OnEnable()
        {
            EnsureBuffer();
            _scanCooldownUnscaled = 0f;

            // ✅ PlayMode에서만 Tick 등록
            if (UnityEngine.Application.isPlaying)
                App.RegisterWhenReady(this);
        }

        private void OnDisable()
        {
            if (UnityEngine.Application.isPlaying)
                App.UnregisterTickable(this);
        }

        private void EnsureBuffer()
        {
            overlapBufferSize = Mathf.Clamp(overlapBufferSize, 8, 256);
            if (_buffer == null || _buffer.Length != overlapBufferSize)
                _buffer = new Collider[overlapBufferSize];
        }

        /// <summary>
        /// ✅ Update 대신 Tick에서 스캔 주기 제어
        /// - unscaledDt 기반(기존 Time.unscaledTime 의도 유지)
        /// - 큰 dt가 들어와도 “한 번만” 스캔 (픽업 자석은 catch-up 불필요)
        /// </summary>
        public void UnscaledFrameTick(float unscaledDt)
        {
            if (unscaledDt <= 0f) return;

            _scanCooldownUnscaled -= unscaledDt;
            if (_scanCooldownUnscaled > 0f) return;

            _scanCooldownUnscaled = Mathf.Max(0.01f, scanInterval);
            ScanOnce();
        }

        private void ScanOnce()
        {
            if (collector == null) return;
            if (magnetTarget == null) return;

            float radius = ComputeRadius();
            Vector3 center = transform.position;

            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                _buffer,
                pickupLayerMask,
                QueryTriggerInteraction.Collide
            );

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
            float r = baseRadius;

            // 권장: Strategy 사용 (FinalWithStatus면 버프/디버프도 반영)
            if (radiusScaling != null && owner != null)
                r = radiusScaling.Evaluate(owner, baseRadius);

            return Mathf.Max(0.1f, r);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float r = Mathf.Max(0.1f, baseRadius);

            if (owner != null && radiusScaling != null)
                r = Mathf.Max(0.1f, radiusScaling.Evaluate(owner, baseRadius));

            Gizmos.color = new Color(0.2f, 0.45f, 1.0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, r);
        }
#endif
    }
}
