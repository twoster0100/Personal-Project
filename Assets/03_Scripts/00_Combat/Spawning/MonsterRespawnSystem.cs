using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MyGame.Application;
using MyGame.Application.Tick;

namespace MyGame.Combat
{
    [DisallowMultipleComponent]
    public sealed class MonsterRespawnSystem : MonoBehaviour, ISimulationTickable
    {
        /// <summary>
        /// ✅ 외부(CombatController 등)에서 "몬스터 리스폰을 이 시스템이 담당 중인지" 빠르게 확인하기 위한 플래그.
        /// - 씬에 MonsterRespawnSystem이 1개 이상 활성화되어 있으면 true
        /// - static이므로 Editor 도메인 리로드/재생 종료 시 값이 남을 수 있어 SubsystemRegistration에서 초기화한다.
        /// </summary>
        public static bool IsActive => s_activeCount > 0;
        private static int s_activeCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            s_activeCount = 0;
        }

        [Header("Wiring")]
        [SerializeField] private SpawnAreaBox spawnArea;

        [Header("Timing")]
        [SerializeField] private float monsterDisableDelay = 0.5f;
        [SerializeField] private float respawnDelayAfterDisable = 0.15f;

        [Header("Spawn - Ground Snap (Recommended)")]
        [Tooltip("스폰 좌표의 Y를 지면에 스냅(레이캐스트)")]
        [SerializeField] private bool snapToGround = true;

        [Tooltip("지면 레이어 마스크 (Ground 레이어 추천)")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Tooltip("레이 시작 높이(유닛). 스폰 후보점 위에서 아래로 쏜다.")]
        [SerializeField] private float groundRayStartHeight = 10f;

        [Tooltip("레이 최대 거리(유닛). 시작높이 + 이 거리만큼 아래로 검사")]
        [SerializeField] private float groundRayDistance = 50f;

        [Tooltip("피벗/콜라이더 보정 외 추가로 띄우고 싶으면(유닛)")]
        [SerializeField] private float extraGroundOffset = 0f;

        [Tooltip("콜라이더 바닥이 지면에 닿도록(피벗이 중앙이어도 파묻힘 방지)")]
        [SerializeField] private bool alignColliderBottomToGround = true;

        [Header("Spawn - Optional NavMesh")]
        [SerializeField] private bool useNavMeshSamplePosition = false;
        [SerializeField] private float navMeshSampleRadius = 1.0f;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

        [Header("Debug")]
        [SerializeField] private bool log = false;

        private readonly List<Entry> _pending = new();

        private sealed class Entry
        {
            public Actor actor;
            public float remain;
            public Phase phase;
        }

        private enum Phase { WaitDisable, WaitRespawn }

        private void Reset()
        {
            if (spawnArea == null) spawnArea = FindObjectOfType<SpawnAreaBox>();
        }

        private void OnEnable()
        {
            s_activeCount++;
            Actor.AnyDied += OnAnyActorDied;
            App.RegisterWhenReady(this);
        }

        private void OnDisable()
        {
            s_activeCount = Mathf.Max(0, s_activeCount - 1);
            Actor.AnyDied -= OnAnyActorDied;
            App.UnregisterTickable(this);
            _pending.Clear();
        }

        private void OnAnyActorDied(ActorDeathEvent e)
        {
            if (e.Victim == null) return;
            if (e.Victim.kind != ActorKind.Monster) return;
            if (spawnArea == null) return;

            // 중복 큐 방지
            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].actor == e.Victim)
                {
                    _pending[i].phase = Phase.WaitDisable;
                    _pending[i].remain = Mathf.Max(0f, monsterDisableDelay);
                    return;
                }
            }

            _pending.Add(new Entry
            {
                actor = e.Victim,
                phase = Phase.WaitDisable,
                remain = Mathf.Max(0f, monsterDisableDelay)
            });

            if (log)
                Debug.Log($"[Respawn] queued: {e.Victim.name} disable in {monsterDisableDelay:0.00}s");
        }

        public void SimulationTick(float dt)
        {
            if (_pending.Count == 0) return;

            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                if (p.actor == null)
                {
                    _pending.RemoveAt(i);
                    continue;
                }

                p.remain -= dt;
                if (p.remain > 0f) continue;

                if (p.phase == Phase.WaitDisable)
                {
                    DisableNow(p.actor);
                    p.phase = Phase.WaitRespawn;
                    p.remain = Mathf.Max(0f, respawnDelayAfterDisable);

                    if (log) Debug.Log($"[Respawn] disabled: {p.actor.name}, respawn in {p.remain:0.00}s");
                    continue;
                }

                RespawnNow(p.actor);
                _pending.RemoveAt(i);
            }
        }

        private void DisableNow(Actor a)
        {
            if (a == null) return;

            var go = a.gameObject;
            if (!go.activeSelf) return;

            a.ReturnToPoolOrDisable();
        }

        private void RespawnNow(Actor a)
        {
            if (a == null) return;
            if (spawnArea == null) return;

            var go = a.gameObject;
            if (!go.activeSelf)
                go.SetActive(true);

            // 1) 후보점
            Vector3 p = spawnArea.GetRandomPoint();

            // 2) NavMesh 보정(선택)
            if (useNavMeshSamplePosition)
            {
                if (NavMesh.SamplePosition(p, out var hit, Mathf.Max(0.1f, navMeshSampleRadius), navMeshAreaMask))
                    p = hit.position;
            }

            // 3) Ground 스냅(추천)
            if (snapToGround)
                p = SnapPointToGround(p, a);

            a.transform.position = p;
            a.RespawnNow();

              //  if (log)
              //   Debug.Log($"[Respawn] {a.name} -> {p}");
        }

        private Vector3 SnapPointToGround(Vector3 p, Actor a)
        {
            Vector3 origin = p + Vector3.up * Mathf.Max(0.1f, groundRayStartHeight);
            float dist = Mathf.Max(0.1f, groundRayStartHeight + groundRayDistance);

            if (Physics.Raycast(origin, Vector3.down, out var hit, dist, groundMask, QueryTriggerInteraction.Ignore))
            {
                float pivotToBottom = 0f;

                if (alignColliderBottomToGround && a != null)
                {
                    var col = a.GetComponent<Collider>();
                    if (col != null)
                    {
                        // 현재 상태에서 "피벗이 콜라이더 바닥보다 얼마나 위에 있는지"를 계산
                        pivotToBottom = a.transform.position.y - col.bounds.min.y;
                        if (pivotToBottom < 0f) pivotToBottom = 0f;
                    }
                }

                p.y = hit.point.y + pivotToBottom + extraGroundOffset;
                return p;
            }

            // 지면 못 맞추면 기존 p 유지
            return p;
        }
    }
}
