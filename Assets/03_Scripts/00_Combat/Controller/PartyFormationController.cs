using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Tick;
using MyGame.Combat;

namespace MyGame.Party
{
    /// <summary>
    /// 홀드 전용 Formation 컨트롤러
    /// - BeginHoldSearch / BeginHoldDefense : 누르는 동안 유지
    /// - EndHold : 손 떼면 즉시 해제 + 자동전투 재개
    /// - Update 금지: ISimulationTickable로만 갱신
    ///
    /// Search(산개):
    /// - "선택된 캐릭터(카메라/컨트롤)"에서 멀어지는 방향(outward)을 기본으로
    /// - 캐릭터별 고유 방향(assigned)을 섞어서 서로 다른 방향으로 퍼지게 한다.
    /// - 선택된 캐릭터는 Search에서 영향을 받지 않는다(강제이동/전투중단 X).
    ///
    /// Defense(집결):
    /// - 선택된 캐릭터(카메라/컨트롤) 기준으로 원형 배치로 모인다.
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
        // Wiring
        // =========================
        [Header("Wiring")]
        [SerializeField] private PartyControlRouter partyControl;

        [Tooltip("있으면 Anchor를 카메라 Follow 타겟으로 우선 사용 (없으면 ControlledActor)")]
        [SerializeField] private LookAheadFollowProxy cameraFollowProxy;

        // =========================
        // Search (Hold Scatter)
        // =========================
        [Header("Search (Hold Scatter)")]
        [Range(0f, 1f)]
        [Tooltip("0=순수 outward(선택 캐릭터에서 멀어짐), 1=순수 고유방향. 추천 0.35~0.65")]
        [SerializeField] private float scatterAssignedWeight = 0.55f;

        [Tooltip("고유 방향 패턴을 회전시키는 오프셋(도). 0이면 기본 분배")]
        [SerializeField] private float searchAngleOffsetDeg = 0f;

        [Tooltip("Search에서 컨트롤(선택) 캐릭터는 강제 이동/전투 중단을 하지 않는다")]
        [SerializeField] private bool excludeControlledFromSearch = true;

        // =========================
        // Defense (Hold Gather)
        // =========================
        [Header("Defense (Hold Gather)")]
        [SerializeField] private bool keepControlledAtCenter = true;
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

        // Search: 멤버별 고유 산개 방향(월드 기준, StartHold 시 고정)
        private Vector3[] _searchAssignedDirs = Array.Empty<Vector3>();

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
            // Play Mode에서만 App Tick 등록(에디터 편집 중 실행 방지)
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
            _searchAssignedDirs = Array.Empty<Vector3>();
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
            {
                int controlledSlot = GetControlledSlot();
                BuildSearchAssignedDirs(controlledSlot);
            }
            else
            {
                BuildDefenseOffsets();
            }

            TickHold(); // 즉시 1회 반응
        }

        private void TickHold()
        {
            float suspendExtend = forcedRefreshUnscaled + extraSuspendPaddingUnscaled;

            if (_hold == HoldType.SearchScatter)
            {
                // ✅ Search Anchor = 선택된 캐릭터(카메라/컨트롤)
                Vector3 anchor = GetSelectedAnchor();
                int controlledSlot = GetControlledSlot();

                float w = Mathf.Clamp01(scatterAssignedWeight);
                float outwardW = 1f - w;

                for (int i = 0; i < _members.Count; i++)
                {
                    var m = _members[i];
                    if (m.tr == null || m.input == null) continue;

                    // ✅ 선택된 캐릭터는 Search에서 영향 X
                    if (excludeControlledFromSearch && m.slot == controlledSlot)
                        continue;

                    Vector3 pos = m.tr.position; pos.y = 0f;

                    Vector3 outward = pos - anchor;
                    outward.y = 0f;

                    Vector3 assigned = (i < _searchAssignedDirs.Length) ? _searchAssignedDirs[i] : Vector3.right;
                    assigned.y = 0f;

                    Vector3 dir01;

                    if (outward.sqrMagnitude < 0.0001f)
                    {
                        // anchor와 거의 겹치면 outward가 의미 없으니 고유방향만
                        dir01 = assigned.sqrMagnitude < 0.0001f ? Vector3.right : assigned.normalized;
                    }
                    else
                    {
                        Vector3 outward01 = outward.normalized;

                        // outward와 assigned가 정반대면(안쪽으로 끌려갈 위험) assigned 뒤집기
                        if (assigned.sqrMagnitude > 0.0001f && Vector3.Dot(outward01, assigned) < 0f)
                            assigned = -assigned;

                        Vector3 assigned01 = assigned.sqrMagnitude < 0.0001f ? outward01 : assigned.normalized;

                        // outward + 고유방향 섞기
                        Vector3 mixed = outward01 * outwardW + assigned01 * w;
                        if (mixed.sqrMagnitude < 0.0001f) mixed = outward01;

                        dir01 = mixed.normalized;
                    }

                    m.input.SetForcedMove(dir01, forcedRefreshUnscaled);
                    m.combat?.SuspendCombatFor(suspendExtend);
                }
            }
            else if (_hold == HoldType.DefenseGather)
            {
                Vector3 anchor = GetSelectedAnchor();

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
        private void BuildSearchAssignedDirs(int controlledSlot)
        {
            int count = _members.Count;
            _searchAssignedDirs = new Vector3[count];

            // non-controlled만 대상으로 고르게 방향 배분
            int others = 0;
            for (int i = 0; i < count; i++)
                if (_members[i].slot != controlledSlot) others++;

            // fallback: 혼자면 의미 없음
            if (others <= 0)
            {
                for (int i = 0; i < count; i++) _searchAssignedDirs[i] = Vector3.right;
                return;
            }

            Vector3 baseDir = GetViewForwardXZ();
            float baseAngle = Mathf.Atan2(baseDir.z, baseDir.x) * Mathf.Rad2Deg;
            baseAngle += searchAngleOffsetDeg;

            float step = 360f / others;
            int otherIndex = 0;

            for (int i = 0; i < count; i++)
            {
                if (_members[i].slot == controlledSlot)
                {
                    _searchAssignedDirs[i] = Vector3.zero; // 사용 안 함
                    continue;
                }

                float ang = baseAngle + step * otherIndex;
                otherIndex++;

                _searchAssignedDirs[i] = AngleToDirXZ(ang);
            }
        }

        private void BuildDefenseOffsets()
        {
            int count = _members.Count;
            _defenseOffsets = new Vector3[count];

            int controlledSlot = GetControlledSlot();

            int others = keepControlledAtCenter ? Mathf.Max(0, count - 1) : count;
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

                if (keepControlledAtCenter && m.slot == controlledSlot)
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

        private int GetControlledSlot()
        {
            return (partyControl != null) ? partyControl.ControlledSlotIndex : -1;
        }

        /// <summary>
        /// ✅ 선택된 캐릭터(카메라/컨트롤) 위치를 앵커로 사용
        /// 1) cameraFollowProxy.Target
        /// 2) partyControl.ControlledActor
        /// 3) fallback: party centroid
        /// </summary>
        private Vector3 GetSelectedAnchor()
        {
            if (cameraFollowProxy != null && cameraFollowProxy.HasTarget && cameraFollowProxy.Target != null)
            {
                Vector3 p = cameraFollowProxy.Target.position;
                p.y = 0f;
                return p;
            }

            if (partyControl != null && partyControl.ControlledActor != null)
            {
                Vector3 p = partyControl.ControlledActor.transform.position;
                p.y = 0f;
                return p;
            }

            // fallback: centroid
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

        private static Vector3 AngleToDirXZ(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        }

        private static Vector3 GetViewForwardXZ()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 f = cam.transform.forward;
                f.y = 0f;
                if (f.sqrMagnitude > 0.0001f) return f.normalized;
            }
            return Vector3.right;
        }
    }
}
