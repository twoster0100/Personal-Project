using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// ✅ Player 전투 의사결정(Brain)
    /// - "근처 몬스터 우선" 자동 타겟 획득
    /// - (옵션) 전투 중 타겟 고정(죽거나 사라지면 재탐색)
    /// - (옵션) 클릭 오버라이드
    ///
    /// 탐지 반경 스케일링(권장):
    /// - Strategy(StatScaledFloatStrategySO)로 분리
    /// - valueSource = FinalWithStatus (버프/디버프가 반경에도 영향을 주는 설계)
    /// </summary>
    public class PlayerBrain : MonoBehaviour, ICombatBrain
    {
        [Header("Current Target (Read/Debug)")]
        public Actor currentTarget;

        [Header("Options")]
        public bool engageWhenHasTarget = true;

        [Header("Auto Targeting")]
        [SerializeField] private bool autoAcquireTarget = true;

        [Tooltip("기본 탐지 반경(스케일링 이전 base)")]
        [SerializeField] private float acquireRadius = 12f;

        [Tooltip("탐지 반경 스케일링 Strategy (권장: FinalWithStatus 기반)")]
        [SerializeField] private StatScaledFloatStrategySO acquireRadiusScaling;

        [Tooltip("전투 중에는 타겟 고정(타겟이 죽거나 사라지면 그때만 재탐색)")]
        [SerializeField] private bool retargetOnlyWhenNoTargetOrDead = true;

        [Tooltip("재탐색 주기(초). 너무 자주 하면 비용↑, 너무 길면 반응↓")]
        [SerializeField] private float reacquireInterval = 0.20f;

        [Tooltip("탐색에 사용할 레이어 마스크(최적화: Monster 전용 레이어 추천)")]
        [SerializeField] private LayerMask targetMask = ~0;

        [Header("Manual Click Override (Optional)")]
        [SerializeField] private bool allowMouseClickOverride = true;

        [Tooltip("클릭으로 타겟 지정 후 이 시간 동안은 자동 전환을 막음(튀는 전환 방지)")]
        [SerializeField] private float manualTargetLockSeconds = 3f;

        // 내부 상태
        private float _nextScanTime;
        private float _manualLockUntil;

        // NonAlloc 버퍼
        private readonly Collider[] _overlapHits = new Collider[64];

        public CombatIntent Decide(Actor self)
        {
            if (self == null) return CombatIntent.None;

            // 0) (옵션) 클릭으로 타겟 지정
            if (allowMouseClickOverride && Input.GetMouseButtonDown(0))
            {
                TryPickTargetByMouse(self);
            }

            // 1) 타겟 유효성 체크
            if (currentTarget != null)
            {
                if (!currentTarget.IsAlive) currentTarget = null;
                else if (!currentTarget.gameObject.activeInHierarchy) currentTarget = null;
            }

            // ✅ "전투 중 타겟 고정" 옵션:
            // currentTarget이 살아있고 고정 규칙이 켜져있으면,
            // (죽거나 없어질 때까지) 재탐색을 하지 않는다.
            bool shouldSkipAcquire =
                retargetOnlyWhenNoTargetOrDead &&
                currentTarget != null &&
                currentTarget.IsAlive &&
                currentTarget.gameObject.activeInHierarchy;

            // 2) 자동 타겟 획득
            if (!shouldSkipAcquire && autoAcquireTarget && Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + Mathf.Max(0.05f, reacquireInterval);

                // 수동 타겟 잠금 중이면 자동 전환 금지
                bool manualLocked = Time.time < _manualLockUntil;

                if (!manualLocked)
                {
                    float radius = ComputeAcquireRadius(self);
                    var best = FindNearestAliveMonster(self, radius);

                    // 근처에 몬스터가 없으면 타겟 해제
                    currentTarget = best;
                }
            }

            // 3) Engage 판단
            if (!engageWhenHasTarget || currentTarget == null || !currentTarget.IsAlive)
                return CombatIntent.None;

            CombatIntent intent;
            intent.Target = currentTarget;
            intent.Engage = true;
            intent.RequestedSkill = null;
            return intent;
        }

        private float ComputeAcquireRadius(Actor self)
        {
            if (self == null) return Mathf.Max(0.1f, acquireRadius);

            // ✅ 권장: Strategy 사용
            if (acquireRadiusScaling != null)
                return Mathf.Max(0.1f, acquireRadiusScaling.Evaluate(self, acquireRadius));

            // (Fallback) Strategy가 없으면 base 그대로
            return Mathf.Max(0.1f, acquireRadius);
        }

        private void TryPickTargetByMouse(Actor self)
        {
            var cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
            {
                var a = hit.collider.GetComponentInParent<Actor>();
                if (a != null && a != self && a.IsAlive && a.kind == ActorKind.Monster)
                {
                    currentTarget = a;
                    _manualLockUntil = Time.time + Mathf.Max(0f, manualTargetLockSeconds);
                }
            }
        }

        private Actor FindNearestAliveMonster(Actor self, float radius)
        {
            Vector3 center = self.transform.position;
            int count = Physics.OverlapSphereNonAlloc(
                center,
                Mathf.Max(0.1f, radius),
                _overlapHits,
                targetMask,
                QueryTriggerInteraction.Ignore
            );

            Actor best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = _overlapHits[i];
                if (col == null) continue;

                var a = col.GetComponentInParent<Actor>();
                if (a == null) continue;
                if (a == self) continue;
                if (!a.IsAlive) continue;
                if (a.kind != ActorKind.Monster) continue;
                if (!a.gameObject.activeInHierarchy) continue;

                Vector3 d = a.transform.position - center;
                d.y = 0f;
                float sqr = d.sqrMagnitude;

                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = a;
                }
            }

            for (int i = 0; i < count; i++) _overlapHits[i] = null;
            return best;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var self = GetComponent<Actor>();
            float r = Mathf.Max(0.1f, acquireRadius);

            if (self != null && acquireRadiusScaling != null)
                r = Mathf.Max(0.1f, acquireRadiusScaling.Evaluate(self, acquireRadius));

            Gizmos.color = new Color(0.2f, 0.45f, 1.0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, r);
        }
#endif
    }
}
