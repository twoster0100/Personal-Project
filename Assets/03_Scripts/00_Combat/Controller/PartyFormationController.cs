using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Tick;
using MyGame.Combat;

namespace MyGame.Party
{
    /// <summary>
    /// PartyFormationController
    /// - UI 버튼(Search/Defense)을 받아 "Formation 명령"을 파티 전체에 적용한다.
    /// - 핵심 아이디어:
    ///   1) MoveInputResolver에 ForcedMove(최상위 우선순위)를 넣어 이동을 강제한다.
    ///   2) CombatController.SuspendCombatFor로 추적/공격 루프를 잠깐 끊는다(전투 중에도 형태변환 수행).
    /// - Update 사용 금지: ISimulationTickable로 30Hz 시뮬 틱에서 갱신한다.
    /// </summary>
    public sealed class PartyFormationController : MonoBehaviour, ISimulationTickable
    {
        private enum CommandType
        {
            None = 0,
            SearchScatter = 1,
            DefenseGather = 2,
        }

        [Header("Wiring")]
        [SerializeField] private PartyControlRouter partyControl;

        [Tooltip("있으면 카메라가 따라가는 타겟(또는 Pivot)을 Defense 앵커로 우선 사용")]
        [SerializeField] private LookAheadFollowProxy cameraFollowProxy;

        [Header("Search (Scatter)")]
        [Tooltip("산개 기준점을 파티 중심점(centroid)으로 할지")]
        [SerializeField] private bool useCentroidAsSearchAnchor = true;

        [SerializeField] private float searchRadius = 3.5f;
        [SerializeField] private float searchAngleOffsetDeg = 0f;
        [SerializeField] private float searchMaxDurationUnscaled = 1.25f;

        [Header("Defense (Gather)")]
        [Tooltip("Defense 앵커(선택 캐릭터) 중앙에 컨트롤 캐릭터를 그대로 둘지")]
        [SerializeField] private bool keepControlledAtCenter = true;

        [SerializeField] private float defenseRadius = 1.2f;
        [SerializeField] private float defenseAngleOffsetDeg = 0f;
        [SerializeField] private float defenseMaxDurationUnscaled = 1.0f;

        [Header("Movement Tuning")]
        [Tooltip("목표 지점 도착 판정 거리(8방향 스냅이면 0.25~0.5 권장)")]
        [SerializeField] private float arriveDistance = 0.35f;

        [Tooltip("ForcedMove 만료를 방지하기 위해 갱신할 유지시간(초). 틱마다 SetForcedMove로 연장한다.")]
        [SerializeField] private float forcedRefreshUnscaled = 0.25f;

        [Header("Combat Suspend")]
        [Tooltip("전투 중단 시간에 여유로 더해줄 패딩(초)")]
        [SerializeField] private float extraSuspendPaddingUnscaled = 0.2f;

        // ---------- runtime state ----------
        private CommandType _cmd = CommandType.None;
        private float _cmdEndUnscaled;

        private readonly List<Member> _members = new();
        private Vector3[] _targets = Array.Empty<Vector3>();

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
            App.UnregisterTickable(this);
        }

        // =========================================================
        // UI Hook (Button OnClick에서 연결할 함수)
        // =========================================================

        public void ToggleSearchScatter()
        {
            if (_cmd == CommandType.SearchScatter) Cancel();
            else StartCommand(CommandType.SearchScatter);
        }

        public void ToggleDefenseGather()
        {
            if (_cmd == CommandType.DefenseGather) Cancel();
            else StartCommand(CommandType.DefenseGather);
        }

        public void Cancel()
        {
            EndCommand(clearCombatSuspension: true);
        }

        // =========================================================
        // Tick
        // =========================================================

        public void SimulationTick(float dt)
        {
            if (_cmd == CommandType.None) return;

            // 시간 초과
            if (Time.unscaledTime > _cmdEndUnscaled)
            {
                EndCommand(clearCombatSuspension: true);
                return;
            }

            bool allArrived = true;

            for (int i = 0; i < _members.Count; i++)
            {
                var m = _members[i];
                if (m.tr == null || m.input == null) continue;

                Vector3 pos = m.tr.position; pos.y = 0f;
                Vector3 target = _targets[i]; target.y = 0f;

                Vector3 delta = target - pos;
                delta.y = 0f;

                float dist = delta.magnitude;

                if (dist <= arriveDistance)
                {
                    // 도착했으면 강제이동을 풀어준다(이후에도 명령이 유지되면 다시 걸 수 있음)
                    m.input.ClearForcedMove();
                }
                else
                {
                    allArrived = false;

                    // 방향만 강제 (속도는 PlayerMover.speed가 담당)
                    Vector3 dir01 = delta / Mathf.Max(0.0001f, dist);
                    m.input.SetForcedMove(dir01, forcedRefreshUnscaled);
                }
            }

            // 모두 도착하면 조기 종료(전투도 즉시 재개)
            if (allArrived)
                EndCommand(clearCombatSuspension: true);
        }

        // =========================================================
        // Command lifecycle
        // =========================================================

        private void StartCommand(CommandType type)
        {
            // 기존 명령이 있으면 깨끗이 종료
            EndCommand(clearCombatSuspension: true);

            CollectMembers();
            if (_members.Count == 0) return;

            _cmd = type;

            float duration = GetDurationUnscaled(type);
            _cmdEndUnscaled = Time.unscaledTime + Mathf.Max(0.05f, duration);

            BuildTargets(type);

            // 전투를 끊고(추적/공격 중단) 이동 형태변환을 수행
            float suspend = duration + extraSuspendPaddingUnscaled;
            for (int i = 0; i < _members.Count; i++)
                _members[i].combat?.SuspendCombatFor(suspend);

            // 시작 즉시 반응성을 위해 1회 즉시 갱신
            SimulationTick(0f);
        }

        private void EndCommand(bool clearCombatSuspension)
        {
            for (int i = 0; i < _members.Count; i++)
            {
                var m = _members[i];
                if (m.input != null) m.input.ClearForcedMove();
                if (clearCombatSuspension && m.combat != null) m.combat.ClearCombatSuspension();
            }

            _members.Clear();
            _targets = Array.Empty<Vector3>();
            _cmd = CommandType.None;
            _cmdEndUnscaled = 0f;
        }

        // =========================================================
        // Members / Targets
        // =========================================================

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

        private float GetDurationUnscaled(CommandType type)
        {
            return type switch
            {
                CommandType.SearchScatter => searchMaxDurationUnscaled,
                CommandType.DefenseGather => defenseMaxDurationUnscaled,
                _ => 0f
            };
        }

        private void BuildTargets(CommandType type)
        {
            _targets = new Vector3[_members.Count];

            Vector3 anchor = type == CommandType.SearchScatter
                ? GetSearchAnchor()
                : GetDefenseAnchor();

            int controlledSlot = (partyControl != null) ? partyControl.ControlledSlotIndex : -1;

            if (type == CommandType.SearchScatter)
            {
                int count = _members.Count;
                for (int i = 0; i < count; i++)
                {
                    float ang = (360f / count) * i + searchAngleOffsetDeg;
                    Vector3 dir = AngleToDirXZ(ang);
                    _targets[i] = anchor + dir * searchRadius;
                }
            }
            else // DefenseGather
            {
                int count = _members.Count;

                // controlled는 중앙 유지 옵션
                int others = keepControlledAtCenter ? Mathf.Max(0, count - 1) : count;
                int otherIndex = 0;

                for (int i = 0; i < count; i++)
                {
                    var m = _members[i];

                    if (keepControlledAtCenter && m.slot == controlledSlot)
                    {
                        _targets[i] = anchor; // 중앙(앵커) 유지
                        continue;
                    }

                    if (others <= 0)
                    {
                        _targets[i] = anchor;
                        continue;
                    }

                    float ang = (360f / others) * otherIndex + defenseAngleOffsetDeg;
                    otherIndex++;

                    Vector3 dir = AngleToDirXZ(ang);
                    _targets[i] = anchor + dir * defenseRadius;
                }
            }
        }

        private Vector3 GetSearchAnchor()
        {
            if (!useCentroidAsSearchAnchor)
                return GetDefenseAnchor();

            // centroid(파티 중심점)
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
            // 1) 카메라 FollowProxy 타겟이 있으면 그걸 우선
            if (cameraFollowProxy != null && cameraFollowProxy.HasTarget && cameraFollowProxy.Target != null)
            {
                Vector3 p = cameraFollowProxy.Target.position;
                p.y = 0f;
                return p;
            }

            // 2) 컨트롤 중인 캐릭터
            if (partyControl != null && partyControl.ControlledActor != null)
            {
                Vector3 p = partyControl.ControlledActor.transform.position;
                p.y = 0f;
                return p;
            }

            // 3) fallback: centroid
            return GetSearchAnchor();
        }

        private static Vector3 AngleToDirXZ(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_targets == null || _targets.Length == 0) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < _targets.Length; i++)
                Gizmos.DrawWireSphere(_targets[i], 0.15f);
        }
#endif
    }
}
