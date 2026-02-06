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
    /// </summary>
    public sealed class PartyFormationController : MonoBehaviour, ISimulationTickable
    {
        private enum HoldType
        {
            None = 0,
            SearchScatter = 1,
            DefenseGather = 2
        }

        [Header("Wiring")]
        [SerializeField] private PartyControlRouter partyControl;

        [Tooltip("있으면 카메라 Follow 타겟(또는 Pivot)을 Defense 앵커로 우선 사용")]
        [SerializeField] private LookAheadFollowProxy cameraFollowProxy;

        [Header("Search (Hold Scatter)")]
        [SerializeField] private bool useCentroidAsSearchAnchor = true;
        [SerializeField] private float searchAngleOffsetDeg = 0f;

        [Header("Defense (Hold Gather)")]
        [SerializeField] private bool keepControlledAtCenter = true;
        [SerializeField] private float defenseRadius = 1.2f;
        [SerializeField] private float defenseAngleOffsetDeg = 0f;

        [Header("Movement Tuning")]
        [SerializeField] private float arriveDistance = 0.35f;

        [Tooltip("ForcedMove 만료 방지용 유지시간(초). 매 틱 SetForcedMove로 연장한다.")]
        [SerializeField] private float forcedRefreshUnscaled = 0.25f;

        [Header("Combat Suspend")]
        [Tooltip("SuspendCombatFor 연장시 여유 패딩(초)")]
        [SerializeField] private float extraSuspendPaddingUnscaled = 0.2f;

        // runtime
        private HoldType _hold = HoldType.None;
        private readonly List<Member> _members = new();
        private Vector3[] _defenseOffsets = Array.Empty<Vector3>();

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

            // 비활성화되면 안전하게 해제
            EndHold();
            App.UnregisterTickable(this);
        }

        // =========================================================
        // ✅ UI에서 호출할 Hold API
        // =========================================================

        public void BeginHoldSearch()
        {
            StartHold(HoldType.SearchScatter);
        }

        public void BeginHoldDefense()
        {
            StartHold(HoldType.DefenseGather);
        }

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
            _hold = HoldType.None;
        }

        // =========================================================
        // Tick
        // =========================================================

        public void SimulationTick(float dt)
        {
            if (_hold == HoldType.None) return;

            // 매 틱: 강제이동 유지 + 전투중단 연장
            TickHold();
        }

        // =========================================================
        // Internal
        // =========================================================

        private void StartHold(HoldType type)
        {
            // 기존 홀드가 있으면 정리 후 시작
            EndHold();

            CollectMembers();
            if (_members.Count == 0) return;

            _hold = type;

            if (_hold == HoldType.DefenseGather)
                BuildDefenseOffsets();

            // 즉시 반응 1회
            TickHold();
        }

        private void TickHold()
        {
            float suspendExtend = forcedRefreshUnscaled + extraSuspendPaddingUnscaled;

            if (_hold == HoldType.SearchScatter)
            {
                Vector3 anchor = GetSearchAnchor();

                for (int i = 0; i < _members.Count; i++)
                {
                    var m = _members[i];
                    if (m.tr == null || m.input == null) continue;

                    Vector3 pos = m.tr.position; pos.y = 0f;

                    // 바깥 방향 = (내 위치 - 중심)
                    Vector3 outward = pos - anchor;
                    outward.y = 0f;

                    // 겹치면 기본 방향(슬롯/인덱스 기반)
                    if (outward.sqrMagnitude < 0.0001f)
                        outward = GetFallbackDirByIndex(i);

                    Vector3 dir01 = outward.normalized;

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
                        // 도착 후에도 Hold 동안은 0 벡터 강제입력으로 고정(오토/조이스틱 방지)
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

        private void BuildDefenseOffsets()
        {
            _defenseOffsets = new Vector3[_members.Count];

            int controlledSlot = (partyControl != null) ? partyControl.ControlledSlotIndex : -1;

            int count = _members.Count;
            int others = keepControlledAtCenter ? Mathf.Max(0, count - 1) : count;
            int otherIndex = 0;

            for (int i = 0; i < count; i++)
            {
                var m = _members[i];

                if (keepControlledAtCenter && m.slot == controlledSlot)
                {
                    _defenseOffsets[i] = Vector3.zero;
                    continue;
                }

                if (others <= 0)
                {
                    _defenseOffsets[i] = Vector3.zero;
                    continue;
                }

                float ang = (360f / others) * otherIndex + defenseAngleOffsetDeg;
                otherIndex++;

                _defenseOffsets[i] = AngleToDirXZ(ang);
            }
        }

        private Vector3 GetSearchAnchor()
        {
            if (!useCentroidAsSearchAnchor)
                return GetDefenseAnchor();

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

            return GetSearchAnchor();
        }

        private Vector3 GetFallbackDirByIndex(int i)
        {
            int count = Mathf.Max(1, _members.Count);
            float ang = (360f / count) * i + searchAngleOffsetDeg;
            return AngleToDirXZ(ang);
        }

        private static Vector3 AngleToDirXZ(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        }
    }
}
