using System.Collections;
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

            public void Tick(float dt) => Current?.Tick(dt);
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
            public abstract void Tick(float dt);
            public virtual void Exit() { }
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

                if (!cc.Intent.Engage || !cc.HasValidTarget())
                    return;

                sm.Change(cc.IsInAttackRange() ? CombatStateId.AttackLoop : CombatStateId.Chase);
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
                    cc.StopMove();
                    sm.Change(CombatStateId.Idle);
                    return;
                }

                // 스킬 우선
                if (cc.TryGetRequestedSkill(out _))
                {
                    cc.StopMove();
                    sm.Change(CombatStateId.CastSkill);
                    return;
                }

                if (cc.IsInAttackRange())
                {
                    cc.StopMove();
                    sm.Change(CombatStateId.AttackLoop);
                    return;
                }

                //  Chase에서만 이동 의도 세팅
                cc.MoveTowardTarget(dt);
            }
        }

        // ======================
        // AttackLoop (DX 기반 공속으로 계속 공격)
        // ======================
        private class AttackLoopState : CombatState
        {
            public AttackLoopState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Enter()
            {
                cc.StopMove();

                // 첫 사거리 진입 시 즉시 1회 공격
                if (cc.Intent.Engage && cc.HasValidTarget() && cc.IsInAttackRange() && cc.IsBasicAttackReady && cc.CanBasicAttackNow())
                {
                    cc.Anim?.TriggerAttack();
                    cc.DoBasicAttack();
                    cc.StartBasicAttackCooldown(); // 다음 공격까지 기다리기 시작
                }
            }

            public override void Tick(float dt)
            {
                cc.StopMove();

                if (!cc.Intent.Engage || !cc.HasValidTarget())
                {
                    // 전투 종료 : 다음 전투를 위해 쿨다운은 0으로 리셋
                    sm.Change(CombatStateId.Idle);
                    return;
                }
                if (cc.TryGetRequestedSkill(out _))
                {
                    sm.Change(CombatStateId.CastSkill);
                    return;
                }
                if (!cc.IsInAttackRange())
                {
                    sm.Change(CombatStateId.Chase);
                    return;
                }

                // 쿨다운이 준비되면 공격
                if (cc.IsBasicAttackReady && cc.CanBasicAttackNow())
                {
                    cc.Anim?.TriggerAttack();
                    cc.DoBasicAttack();
                    cc.StartBasicAttackCooldown();
                }
            }
        }

        // ======================
        // CastSkill (캐스팅 후 스킬 실행)
        // ======================
        private class CastSkillState : CombatState
        {
            private Coroutine routine;

            public CastSkillState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Enter()
            {
                cc.StopMove();
                cc.Anim?.TriggerCast();
                routine = cc.StartCoroutine(CastRoutine());
            }

            public override void Exit()
            {
                if (routine != null) cc.StopCoroutine(routine);
                routine = null;
            }

            public override void Tick(float dt)
            {
                cc.StopMove();
            }

            private IEnumerator CastRoutine()
            {
                if (!cc.TryGetRequestedSkill(out var skill))
                {
                    ReturnToFlow();
                    yield break;
                }

                float castTime = Mathf.Max(0f, skill.castTime);
                if (castTime > 0f)
                    yield return new WaitForSeconds(castTime);

                cc.ExecuteSkill(skill);
                ReturnToFlow();
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
        // Dead (플레이어: Respawn, 몬스터: Disable/Pool)
        // ======================
        private class DeadState : CombatState
        {
            private Coroutine routine;

            public DeadState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Enter()
            {
                cc.StopMove();
                routine = cc.StartCoroutine(DeadRoutine());
            }

            public override void Exit()
            {
                if (routine != null) cc.StopCoroutine(routine);
                routine = null;
            }

            public override void Tick(float dt) { }

            private IEnumerator DeadRoutine()
            {
                if (cc.Self.useRespawn)
                {
                    yield return new WaitForSeconds(cc.Self.respawnDelay);
                    sm.Change(CombatStateId.Respawn);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
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
