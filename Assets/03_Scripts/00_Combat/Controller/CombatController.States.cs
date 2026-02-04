using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Combat
{
    public partial class CombatController
    {
        // ======================
        // FSM Core
        // ======================
        private class CombatStateMachine
        {
            private readonly Dictionary<CombatStateId, CombatState> states = new();
            private readonly CombatController owner;

            public CombatState Current { get; private set; }
            public CombatStateId CurrentId { get; private set; }

            public CombatStateMachine(CombatController owner) => this.owner = owner;

            public void Add(CombatStateId id, CombatState state) => states[id] = state;

            public void Change(CombatStateId id)
            {
                if (CurrentId == id && Current != null) return;
                if (!states.TryGetValue(id, out var next)) return;

                //Debug.Log($"[FSM] {owner.name}: {CurrentId} -> {id}");

                Current?.Exit();
                Current = next;
                CurrentId = id;
                Current.Enter();
            }

            public void Tick(float dt)
            {
                Current?.Tick(dt);
            }
        }

        private abstract class CombatState
        {
            protected readonly CombatStateMachine sm;
            protected readonly CombatController cc;

            protected CombatState(CombatStateMachine sm, CombatController cc)
            {
                this.sm = sm;
                this.cc = cc;
            }

            public virtual void Enter() { }
            public virtual void Exit() { }
            public virtual void Tick(float dt) { }
        }

        // ======================
        // Idle
        // ======================
        private class IdleState : CombatState
        {
            public IdleState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Tick(float dt)
            {
                cc.StopMove();

                if (cc.Intent.Engage && cc.HasValidTarget())
                {
                    if (cc.IsInAttackRange())
                        sm.Change(CombatStateId.AttackLoop);
                    else
                        sm.Change(CombatStateId.Chase);
                }
            }
        }

        // ======================
        // Chase
        // ======================
        private class ChaseState : CombatState
        {
            public ChaseState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Tick(float dt)
            {
                if (!cc.Intent.Engage || !cc.HasValidTarget())
                {
                    sm.Change(CombatStateId.Idle);
                    return;
                }

                // 스킬 캐스팅 요청이 있으면 캐스팅 상태로
                if (cc.TryGetRequestedSkill(out var _))
                {
                    sm.Change(CombatStateId.CastSkill);
                    return;
                }

                if (cc.IsInAttackRange())
                {
                    cc.StopMove();
                    sm.Change(CombatStateId.AttackLoop);
                    return;
                }

                cc.MoveTowardTarget(dt);
            }
        }

        // ======================
        // AttackLoop
        // ======================
        private class AttackLoopState : CombatState
        {
            public AttackLoopState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Tick(float dt)
            {
                if (!cc.Intent.Engage || !cc.HasValidTarget())
                {
                    sm.Change(CombatStateId.Idle);
                    return;
                }

                // 스킬 캐스팅 요청이 있으면 캐스팅 상태로
                if (cc.TryGetRequestedSkill(out var _))
                {
                    sm.Change(CombatStateId.CastSkill);
                    return;
                }

                if (!cc.IsInAttackRange())
                {
                    sm.Change(CombatStateId.Chase);
                    return;
                }

                cc.StopMove();

                if (cc.CanBasicAttackNow())
                    cc.DoBasicAttack();
            }
        }

        // ======================
        // CastSkill (캐스팅 후 스킬 실행) - ✅ Tick 기반 타이머(코루틴 제거)
        // ======================
        private class CastSkillState : CombatState
        {
            private SkillDefinitionSO _skill;
            private float _remain;

            public CastSkillState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Enter()
            {
                cc.StopMove();
                cc.Anim?.TriggerCast();

                // 요청된 스킬이 없으면 즉시 흐름 복귀
                if (!cc.TryGetRequestedSkill(out _skill) || _skill == null)
                {
                    _skill = null;
                    _remain = 0f;
                    ReturnToFlow();
                    return;
                }

                _remain = Mathf.Max(0f, _skill.castTime);

                // 캐스팅 시간이 0이면 즉시 실행
                if (_remain <= 0f)
                {
                    cc.ExecuteSkill(_skill);
                    _skill = null;
                    ReturnToFlow();
                }
            }

            public override void Tick(float dt)
            {
                cc.StopMove();

                // 캐스팅 중에 스킬이 취소/소거되면 흐름 복귀
                if (_skill == null)
                {
                    ReturnToFlow();
                    return;
                }

                _remain -= dt;
                if (_remain > 0f) return;

                cc.ExecuteSkill(_skill);
                _skill = null;
                ReturnToFlow();
            }

            public override void Exit()
            {
                _skill = null;
                _remain = 0f;
            }

            private void ReturnToFlow()
            {
                if (!cc.Intent.Engage || !cc.HasValidTarget())
                    sm.Change(CombatStateId.Idle);
                else if (cc.IsInAttackRange())
                    sm.Change(CombatStateId.AttackLoop);
                else
                    sm.Change(CombatStateId.Chase);
            }
        }

        // ======================
        // Stunned (Stun 효과가 forceStateTransition으로 강제)
        // ======================
        private class StunnedState : CombatState
        {
            public StunnedState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Tick(float dt)
            {
                cc.StopMove();

                if (cc.Self == null || !cc.Self.IsAlive)
                {
                    sm.Change(CombatStateId.Dead);
                    return;
                }

                // 스턴(강제 상태)이 풀리면 Idle로 복귀
                var st = cc.Self.Status;
                if (st == null || !st.TryGetForcedState(out var forced) || forced != CombatStateId.Stunned)
                {
                    sm.Change(CombatStateId.Idle);
                }
            }
        }

        // ======================
        // Dead (플레이어: Respawn, 몬스터: Disable/Pool) - ✅ Tick 기반 타이머(코루틴 제거)
        // ======================
        private class DeadState : CombatState
        {
            private float _remain;
            private bool _done;

            public DeadState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Enter()
            {
                cc.StopMove();

                _done = false;

                // ✅ 외부 몬스터 리스폰 시스템이 켜져 있으면(몬스터 한정)
                // CombatController는 Disable/Respawn을 하지 않고, 시스템이 처리하도록 둔다.
                if (cc.Self != null && cc.Self.kind == ActorKind.Monster && MonsterRespawnSystem.IsActive)
                {
                    _remain = 0f;
                    return;
                }

                // ✅ 기본 처리(플레이어 Respawn / 몬스터 Disable)
                // - useRespawn=true면 respawnDelay 후 RespawnState로
                // - useRespawn=false면 즉시 Disable(필요하면 별도 딜레이 필드로 확장 가능)
                _remain = (cc.Self != null && cc.Self.useRespawn) ? Mathf.Max(0f, cc.Self.respawnDelay) : 0f;
            }

            public override void Tick(float dt)
            {
                if (_done) return;

                _remain -= dt;
                if (_remain > 0f) return;

                Complete();
            }

            public override void Exit()
            {
                _remain = 0f;
                _done = false;
            }

            private void Complete()
            {
                if (_done) return;
                _done = true;

                if (cc.Self != null && cc.Self.kind == ActorKind.Monster && MonsterRespawnSystem.IsActive) return;

                if (cc.Self.useRespawn)
                {
                    sm.Change(CombatStateId.Respawn);
                }
                else
                {
                    // ⚠️ 여기서 SetActive(false)가 발생할 수 있으므로,
                    // TickScheduler는 Tick 중 Register/Unregister를 안전하게 처리해야 한다.
                    cc.Self.ReturnToPoolOrDisable();
                }
            }
        }

        // ======================
        // Respawn
        // ======================
        private class RespawnState : CombatState
        {
            public RespawnState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Enter()
            {
                cc.StopMove();

                // ✅ 리스폰 지점(옵션)
                // - CombatController는 "플레이어 리스폰"에 주로 사용
                // - 지형 스냅이 필요하면 SpawnAreaBox의 size.y=0으로 두거나,
                //   MonsterRespawnSystem의 SnapToGround 로직을 별도 유틸로 공용화하는 것을 권장
                if (cc.respawnArea != null && cc.respawnArea.TryGetPoint(out var p))
                    cc.transform.position = p;

                cc.Self.RespawnNow();
                sm.Change(CombatStateId.Idle);
            }

            public override void Tick(float dt) { }
        }
    }
}
