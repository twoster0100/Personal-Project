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

                // 플레이어(혹은 useRespawn=true)는 respawnDelay 후 RespawnState로 이동
                // 몬스터는 짧은 연출 딜레이 후 풀/비활성 처리(기본 0.5s)
                _remain = cc.Self != null && cc.Self.useRespawn
                    ? Mathf.Max(0f, cc.Self.respawnDelay)
                    : 0.5f;

                if (_remain <= 0f)
                    Complete();
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

                if (cc.Self == null) return;

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
                cc.Self.RespawnNow();
                sm.Change(CombatStateId.Idle);
            }

            public override void Tick(float dt) { }
        }
    }
}
