using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Tick;
using MyGame.Combat;

namespace MyGame.Party
{
    /// <summary>
    /// 홀드 전용 Formation 컨트롤러 (정리 버전)
    /// - BeginHoldSearch / BeginHoldDefense : 누르는 동안 유지
    /// - EndHold : 손 떼면 즉시 해제 + 자동전투 재개
    /// - Update 금지: ISimulationTickable로만 갱신
    ///
    /// Search는 "outward(중심->바깥) + 고유방향"을 섞어서
    /// 캐릭터들이 서로 다른 방향으로 자연스럽게 산개하도록 만든다.
    /// </summary>
    public sealed class PartyFormationController : MonoBehaviour, ISimulationTickable
    {
        private enum HoldType
        {
            None = 0,
            SearchScatter = 1,
            DefenseGather = 2
        }

        // =========================
        // Wiring (필수/옵션)
        // =========================
        [Header("Wiring")]
        [SerializeField] private PartyControlRouter partyControl;

        [Tooltip("있으면 Defense 앵커를 카메라 Follow 타겟으로 우선 사용 (없으면 ControlledActor)")]
        [SerializeField] private LookAheadFollowProxy cameraFollowProxy;

        // =========================
        // Search (Hold Scatter)
        // =========================
        [Header("Search (Hold Scatter)")]
        [Range(0f, 1f)]
        [Tooltip("0=순수 outward, 1=순수 고유방향. 추천 0.4~0.7")]
        [SerializeField] private float scatterAssignedWeight = 0.55f;

        // =========================
        // Defense (Hold Gather)
        // =========================
        [Header("Defense (Hold Gather)")]
        [SerializeField] private float defenseRadius = 1.2f;

        // =========================
        // Tuning
        // =========================
        [Header("Tuning")]
        [SerializeField] private float arriveDistance = 0.35f;

        [Tooltip("ForcedMove 만료 방지용 유지시간(초). 매 틱 SetForcedMove로 연장한다.")]
        [SerializeField] private float forcedRefreshUnscaled = 0.25f;

        [Tooltip("SuspendCombatFor 연장시 여유 패딩(초)")]
        [SerializeField] private float extraSuspendPaddingUnscaled = 0.2f;

        // runtime
        private HoldType _hold = HoldType.None;
        private readonly List<Member> _members = new();

        // Defense: 각 멤버가 차지할 원형 배치 방향(Controlled는 0)
        private Vector3[] _defenseOffsets = Array.Empty<Vector3>();

        // Search: 멤버별 고유 산개 방향(월드 기준 고정)
        private Vector3[] _scatterDirs = Array.Empty<Vector3>();

        private struct Member
        {
            public int slot;
            public Transform tr;
            public MoveInputResolver input;
            public CombatController combat;
        }

        private void Awake()
        {
            if (partyControl == null) partyControl = FindObjectOfType<PartyControlRouter>();
            if (cameraFollowProxy == null) cameraFollowProxy = FindObjectOfType<LookAheadFollowProxy>();
        }

        private void OnEnable()
        {
            if (!UnityEngine.Application.isPlaying) return;
            App.RegisterWhenReady(this);
        }

        private void OnDisable()
        {
            if (!UnityEngine.Application.isPlaying) return;

            EndHold();
            App.UnregisterTickable(this);
        }

        // =========================================================
        //  UI에서 호출할 Hold API
        // =========================================================

        public void BeginHoldSearch() => StartHold(HoldType.SearchScatter);
        public void BeginHoldDefense() => StartHold(HoldType.DefenseGather);

        public void EndHold()
        {
            if (_hold == HoldType.None) return;

            // 강제이동/전투중단 해제
            for (int i = 0; i < _members.Count; i++)
            {
                var m = _members[i];
                m.input?.ClearForcedMove();
                m.combat?.ClearCombatSuspension();
            }

            _members.Clear();
            _defenseOffsets = Array.Empty<Vector3>();
            _scatterDirs = Array.Empty<Vector3>();
            _hold = HoldType.None;
        }

        // =========================================================
        // Tick
        // =========================================================

        public void SimulationTick(float dt)
        {
            if (_hold == HoldType.None) return;
            TickHold();
        }

        // =========================================================
        // Internal
        // =========================================================

        private void StartHold(HoldType type)
        {
            EndHold();

            CollectMembers();
            if (_members.Count == 0) return;

            _hold = type;

            if (_hold == HoldType.SearchScatter)
                BuildScatterDirs();
            else
                BuildDefenseOffsets();

            TickHold(); // 즉시 1회 반응
        }

        private void TickHold()
        {
            float suspendExtend = forcedRefreshUnscaled + extraSuspendPaddingUnscaled;

            if (_hold == HoldType.SearchScatter)
            {
                Vector3 anchor = GetSearchAnchor();
                float w = Mathf.Clamp01(scatterAssignedWeight);
                float outwardW = 1f - w;

                for (int i = 0; i < _members.Count; i++)
                {
                    var m = _members[i];
                    if (m.tr == null || m.input == null) continue;

                    Vector3 pos = m.tr.position; pos.y = 0f;

                    // outward = (내 위치 - 중심)
                    Vector3 outward = pos - anchor;
                    outward.y = 0f;

                    Vector3 assigned = (i < _scatterDirs.Length) ? _scatterDirs[i] : Vector3.right;
                    assigned.y = 0f;

                    Vector3 dir01;

                    if (outward.sqrMagnitude < 0.0001f)
                    {
                        // 중심과 거의 겹치면 outward가 의미 없으니 고유방향만 사용
                        dir01 = assigned.normalized;
                    }
                    else
                    {
                        Vector3 outward01 = outward.normalized;

                        // outward와 assigned가 정반대면(안쪽으로 끌려갈 위험) assigned를 뒤집어준다
                        if (Vector3.Dot(outward01, assigned) < 0f)
                            assigned = -assigned;

                        //   outward + 고유방향
                        Vector3 mixed = outward01 * outwardW + assigned.normalized * w;
                        if (mixed.sqrMagnitude < 0.0001f) mixed = outward01;

                        dir01 = mixed.normalized;
                    }

                    m.input.SetForcedMove(dir01, forcedRefreshUnscaled);
                    m.combat?.SuspendCombatFor(suspendExtend);
                }
            }
            else if (_hold == HoldType.DefenseGather)
            {
                Vector3 anchor = GetDefenseAnchor();

                for (int i = 0; i < _members.Count; i++)
                {
                    var m = _members[i];
                    if (m.tr == null || m.input == null) continue;

                    Vector3 pos = m.tr.position; pos.y = 0f;

                    Vector3 offsetDir = (i < _defenseOffsets.Length) ? _defenseOffsets[i] : Vector3.zero;
                    offsetDir.y = 0f;

                    Vector3 target = anchor + offsetDir * defenseRadius;
                    target.y = 0f;

                    Vector3 delta = target - pos;
                    delta.y = 0f;

                    float dist = delta.magnitude;

                    if (dist <= arriveDistance)
                    {
                        // 도착 후에도 Hold 동안은 0 벡터 강제입력으로 "고정"
                        m.input.SetForcedMove(Vector3.zero, forcedRefreshUnscaled);
                    }
                    else
                    {
                        Vector3 dir01 = delta / Mathf.Max(0.0001f, dist);
                        m.input.SetForcedMove(dir01, forcedRefreshUnscaled);
                    }

                    m.combat?.SuspendCombatFor(suspendExtend);
                }
            }
        }

        // -------------------------
        // Build dirs
        // -------------------------

        private void BuildScatterDirs()
        {
            int count = _members.Count;
            _scatterDirs = new Vector3[count];

            // 화면 기준으로 퍼지는 느낌: 카메라 forward를 XZ로 투영한 방향을 기준축으로 사용
            Vector3 baseDir = GetViewForwardXZ();
            float baseAngle = Mathf.Atan2(baseDir.z, baseDir.x) * Mathf.Rad2Deg;

            float step = 360f / Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                float ang = baseAngle + step * i;
                _scatterDirs[i] = AngleToDirXZ(ang);
            }
        }

        private void BuildDefenseOffsets()
        {
            int count = _members.Count;
            _defenseOffsets = new Vector3[count];

            int controlledSlot = (partyControl != null) ? partyControl.ControlledSlotIndex : -1;

            // Controlled는 중앙(0), 나머지는 원형 배치
            int others = Mathf.Max(0, count - 1);
            if (others <= 0)
            {
                for (int i = 0; i < count; i++) _defenseOffsets[i] = Vector3.zero;
                return;
            }

            Vector3 baseDir = GetViewForwardXZ();
            float baseAngle = Mathf.Atan2(baseDir.z, baseDir.x) * Mathf.Rad2Deg;

            float step = 360f / others;
            int otherIndex = 0;

            for (int i = 0; i < count; i++)
            {
                var m = _members[i];

                if (m.slot == controlledSlot)
                {
                    _defenseOffsets[i] = Vector3.zero;
                    continue;
                }

                float ang = baseAngle + step * otherIndex;
                otherIndex++;

                _defenseOffsets[i] = AngleToDirXZ(ang);
            }
        }

        // -------------------------
        // Member collection / anchors
        // -------------------------

        private void CollectMembers()
        {
            _members.Clear();

            if (partyControl == null)
            {
                Debug.LogWarning($"{nameof(PartyFormationController)}: PartyControlRouter is null.");
                return;
            }

            int n = Mathf.Max(0, partyControl.PartySize);
            for (int slot = 0; slot < n; slot++)
            {
                var tr = partyControl.GetMember(slot);
                if (tr == null) continue;

                var input = tr.GetComponent<MoveInputResolver>();
                var combat = tr.GetComponent<CombatController>();

                if (input == null)
                {
                    Debug.LogWarning($"[Formation] slot {slot} missing MoveInputResolver: {tr.name}");
                    continue;
                }

                _members.Add(new Member
                {
                    slot = slot,
                    tr = tr,
                    input = input,
                    combat = combat
                });
            }
        }

        private Vector3 GetSearchAnchor()
        {
            // Search 기준점은 항상 centroid
            if (_members.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int c = 0;

            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].tr == null) continue;
                sum += _members[i].tr.position;
                c++;
            }

            if (c <= 0) return Vector3.zero;

            Vector3 center = sum / c;
            center.y = 0f;
            return center;
        }

        private Vector3 GetDefenseAnchor()
        {
            // 1) 카메라 Follow 타겟이 있으면 그쪽이 우선
            if (cameraFollowProxy != null && cameraFollowProxy.HasTarget && cameraFollowProxy.Target != null)
            {
                Vector3 p = cameraFollowProxy.Target.position;
                p.y = 0f;
                return p;
            }

            // 2) 없으면 ControlledActor 기준
            if (partyControl != null && partyControl.ControlledActor != null)
            {
                Vector3 p = partyControl.ControlledActor.transform.position;
                p.y = 0f;
                return p;
            }

            // 3) 마지막 fallback: centroid
            return GetSearchAnchor();
        }

        private static Vector3 AngleToDirXZ(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        }

        private static Vector3 GetViewForwardXZ()
        {
            // Camera.main이 있으면 "화면 기준" 방향을 잡기 좋음
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 f = cam.transform.forward;
                f.y = 0f;
                if (f.sqrMagnitude > 0.0001f) return f.normalized;
            }

            // 없으면 월드 +X 기준
            return Vector3.right;
        }
    }
}
