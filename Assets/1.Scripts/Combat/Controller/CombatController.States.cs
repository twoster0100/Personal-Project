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

                Debug.Log($"[FSM] {owner.name}: {CurrentId} -> {id}");

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
                if (cc.Intent.Engage && cc.HasValidTarget())
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
                    sm.Change(CombatStateId.Idle);
                    return;
                }

                // 스킬 우선
                if (cc.TryGetRequestedSkill(out _))
                {
                    sm.Change(CombatStateId.CastSkill);
                    return;
                }

                if (cc.IsInAttackRange())
                {
                    sm.Change(CombatStateId.AttackLoop);
                    return;
                }

                cc.MoveTowardTarget(dt);
            }
        }

        // ======================
        // AttackLoop (DX 기반 공속으로 지속 공격)
        // ======================
        private class AttackLoopState : CombatState
        {
            private float timer;

            public AttackLoopState(CombatStateMachine sm, CombatController cc) : base(sm, cc) { }

            public override void Enter() => timer = 0f; // 진입 즉시 1타

            public override void Tick(float dt)
            {
                if (!cc.Intent.Engage || !cc.HasValidTarget())
                {
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

                timer -= dt;
                if (timer <= 0f)
                {
                    cc.DoBasicAttack();
                    timer = cc.Self.GetAttackInterval();
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
                routine = cc.StartCoroutine(CastRoutine());
            }

            public override void Exit()
            {
                if (routine != null) cc.StopCoroutine(routine);
                routine = null;
            }

            public override void Tick(float dt) { }

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
                if (cc.Self == null || !cc.Self.IsAlive)
                {
                    sm.Change(CombatStateId.Dead);
                    return;
                }

                // ✅ 스턴(강제 상태)이 풀리면 Idle로 복귀
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
                cc.Self.RespawnNow();
                sm.Change(CombatStateId.Idle);
            }

            public override void Tick(float dt) { }
        }
    }
}
