using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    ///  Player 전투 의사결정(Brain)
    /// -  근처 몬스터 우선 자동 타겟 획득
    /// -  클릭 오버라이드
    /// -  BF에 따라 탐지 반경 스케일링 (PickupMagnet과 동일한 방식)
    /// -  탐지 반경 Gizmo 표시
    /// </summary>
    public class PlayerBrain : MonoBehaviour, ICombatBrain
    {
        [Header("Current Target (Read/Debug)")]
        public Actor currentTarget;

        [Header("Options")]
        public bool engageWhenHasTarget = true;

        [Header("Auto Targeting")]
        [SerializeField] private bool autoAcquireTarget = true;

        [Tooltip("기본 탐지 반경(Base). 실제 사용 반경은 BF 스케일링이 켜져 있으면 더 커짐.")]
        [SerializeField] private float acquireRadius = 50f;

        [Tooltip("재탐색 주기(초). 너무 자주 하면 비용↑, 너무 길면 반응↓")]
        [SerializeField] private float reacquireInterval = 0.5f;

        [Tooltip("현재 타겟보다 '이만큼' 더 가까워야 타겟을 바꿈 (예: 1.25면 25% 더 가까울 때만 전환)")]
        [SerializeField] private float switchIfCloserRatio = 1.25f;

        [Tooltip("탐색에 사용할 레이어 마스크(기본 Everything). 최적화하려면 Monster 전용 레이어 추천")]
        [SerializeField] private LayerMask targetMask = ~0;

        [Header("Acquire Radius - BF Scaling (like PickupMagnet)")]
        [SerializeField] private bool scaleRadiusByBF = true;

        [Tooltip("BF 1당 탐지 반경 증가량. (PickupMagnet은 0.01, 전투 탐지는 보통 더 크게 시작하는 편)")]
        [SerializeField] private float radiusPerBF = 0.01f;

        [Tooltip("BF 계산 방식: true면 Stats.GetTotalStatLevel(BF), false면 Actor.GetFinalStat(BF)")]
        [SerializeField] private bool useBFLevelSum = true;

        [Header("Manual Click Override")]
        [SerializeField] private bool allowMouseClickOverride = true;

        [Tooltip("클릭으로 타겟 지정 후 이 시간 동안은 자동 전환을 막음(어글튀는 전환 방지)")]
        [SerializeField] private float manualTargetLockSeconds = 3f;

        [Header("Debug (Read Only)")]
        [SerializeField] private float debugComputedRadius;

        [Header("Gizmos")]
        [SerializeField] private bool drawAcquireRadiusGizmo = true;

        // 내부 상태
        private float _nextScanTime;
        private float _manualLockUntil;

        // NonAlloc 버퍼 (필요 시 늘리기)
        private readonly Collider[] _overlapHits = new Collider[64];

        public CombatIntent Decide(Actor self)
        {
            if (self == null) return CombatIntent.None;

            // 0) (옵션) 클릭으로 타겟 지정
            if (allowMouseClickOverride && Input.GetMouseButtonDown(0))
            {
                TryPickTargetByMouse(self);
            }

            // 1) 타겟 유효성 체크 (죽었거나 사라졌으면 해제)
            if (currentTarget == null || !currentTarget.IsAlive)
            {
                currentTarget = null;
            }

            // 2) 자동 타겟 획득/전환
            if (autoAcquireTarget && Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + Mathf.Max(0.05f, reacquireInterval);

                // 수동 타겟 잠금 중이면 자동 전환 금지
                bool manualLocked = Time.time < _manualLockUntil;

                if (!manualLocked)
                {
                    float radius = ComputeAcquireRadius(self);
                    debugComputedRadius = radius;

                    var best = FindNearestAliveMonster(self, radius);

                    if (best != null)
                    {
                        // 현재 타겟이 없으면 즉시 채택
                        if (currentTarget == null)
                        {
                            currentTarget = best;
                        }
                        else if (currentTarget != best)
                        {
                            // "근처 우선" 전환 조건:
                            float curSqr = (currentTarget.transform.position - self.transform.position).sqrMagnitude;
                            float bestSqr = (best.transform.position - self.transform.position).sqrMagnitude;

                            float ratio = Mathf.Max(1.01f, switchIfCloserRatio);
                            if (bestSqr * (ratio * ratio) < curSqr)
                            {
                                currentTarget = best;
                            }
                        }
                    }
                    else
                    {
                        currentTarget = null;
                    }
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
            float baseR = Mathf.Max(0.1f, acquireRadius);
            if (!scaleRadiusByBF || self == null) return baseR;

            float bf = 0f;

            // PickupMagnet과 같은 방식으로 BF를 해석할 수 있도록 옵션 제공
            if (useBFLevelSum && self.Stats != null)
            {
                bf = self.Stats.GetTotalStatLevel(StatId.BF);
            }
            else
            {
                bf = self.GetFinalStat(StatId.BF);
            }

            float r = baseR + radiusPerBF * Mathf.Max(0f, bf);
            return Mathf.Max(0.1f, r);
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
            int count = Physics.OverlapSphereNonAlloc(center, Mathf.Max(0.1f, radius), _overlapHits, targetMask, QueryTriggerInteraction.Ignore);

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
                d.y = 0f; // 수평 거리 기준
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
            if (!drawAcquireRadiusGizmo) return;

            var self = GetComponent<Actor>();
            float r = Mathf.Max(0.1f, acquireRadius);

            // 에디터에서도 동일 계산 (런타임과 맞추기)
            if (self != null && scaleRadiusByBF)
            {
                float bf = 0f;
                if (useBFLevelSum && self.Stats != null) bf = self.Stats.GetTotalStatLevel(StatId.BF);
                else bf = self.GetFinalStat(StatId.BF);

                r = Mathf.Max(0.1f, acquireRadius + radiusPerBF * Mathf.Max(0f, bf));
            }

            Gizmos.DrawWireSphere(transform.position, r);
        }
#endif
    }
}
