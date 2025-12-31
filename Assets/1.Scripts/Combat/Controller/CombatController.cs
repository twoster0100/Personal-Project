
using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// ✅ 공용 전투 실행기(FSM + Strategy)
    /// - Brain(Player/Monster)이 만든 CombatIntent를 받아서 "상태머신"을 진행
    /// - 공격/스킬 실행은 Strategy로 위임
    /// - StatusController(버프/디버프)가 행동 제한/강제상태(Stun)를 제어
    /// </summary>
    public partial class CombatController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Actor self;
        [SerializeField] private MonoBehaviour brainComponent; // ICombatBrain 구현체
        private ICombatBrain brain;

        [Header("Strategies")]
        public IBasicAttackStrategy basicAttackStrategy = new MeleeBasicAttackStrategy();
        public ISkillSelectorStrategy autoSkillSelector = new FirstReadySkillSelector();
        public ISkillExecutorStrategy skillExecutor = new InstantDamageSkillExecutor();

        private CombatStateMachine fsm;

        internal CombatIntent Intent { get; private set; }
        internal Actor Self => self;

        private void Reset()
        {
            self = GetComponent<Actor>();
        }

        private void Awake()
        {
            if (self == null) self = GetComponent<Actor>();
            brain = brainComponent as ICombatBrain;

            fsm = new CombatStateMachine(this);
            fsm.Add(CombatStateId.Idle, new IdleState(fsm, this));
            fsm.Add(CombatStateId.Chase, new ChaseState(fsm, this));
            fsm.Add(CombatStateId.AttackLoop, new AttackLoopState(fsm, this));
            fsm.Add(CombatStateId.CastSkill, new CastSkillState(fsm, this));
            fsm.Add(CombatStateId.Stunned, new StunnedState(fsm, this));
            fsm.Add(CombatStateId.Dead, new DeadState(fsm, this));
            fsm.Add(CombatStateId.Respawn, new RespawnState(fsm, this));

            fsm.Change(CombatStateId.Idle);
        }

        private void Update()
        {
            if (self == null) return;

            // 1) 죽었으면 Dead로 고정
            if (!self.IsAlive)
            {
                fsm.Change(CombatStateId.Dead);
                fsm.Tick(Time.deltaTime);
                return;
            }

            // 2) Brain이 의사결정 (플레이어/몬스터 차이점)
            Intent = (brain != null) ? brain.Decide(self) : CombatIntent.None;

            // 3) (선택) 몬스터 자동 스킬 선택
            if (Intent.Engage && Intent.Target != null && Intent.RequestedSkill == null && self.kind == ActorKind.Monster)
            {
                var picked = autoSkillSelector.SelectSkill(self, Intent.Target);
                if (picked != null)
                {
                    Intent = new CombatIntent
                    {
                        Target = Intent.Target,
                        Engage = true,
                        RequestedSkill = picked
                    };
                }
            }

            // 4) ✅ 강제 상태 전이(스턴 등)가 있으면 여기서만 강제로 바꾼다
            //    - "Update 전체를 막는 조건"이 되면 안 됨!!
            if (self.Status != null && self.Status.TryGetForcedState(out var forced))
            {
                fsm.Change(forced);
            }

            // 5) 상태머신 진행
            fsm.Tick(Time.deltaTime);
        }
        // =========================
        // 상태들이 사용하는 유틸
        // =========================
        internal bool HasValidTarget()
            => Intent.Target != null && Intent.Target.IsAlive;

        internal float DistanceToTarget()
        {
            if (!HasValidTarget()) return float.MaxValue;
            return Vector3.Distance(self.transform.position, Intent.Target.transform.position);
        }

        internal bool IsInAttackRange()
            => HasValidTarget() && DistanceToTarget() <= self.attackRange;

        internal void MoveTowardTarget(float dt)
        {
            // ✅ 이동불가(스턴/루트/피격이동불가 등)
            if (self.Status != null && !self.Status.CanMove()) return;
            if (!HasValidTarget()) return;

            Vector3 dir = Intent.Target.transform.position - self.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.001f) return;

            dir.Normalize();
            self.transform.position += dir * self.walkSpeed * dt;
        }

        internal void DoBasicAttack()
        {
            if (!HasValidTarget()) return;

            // ✅ 기본공격불가
            if (self.Status != null && !self.Status.CanBasicAttack()) return;

            // ✅ 물리공격불가(Physical 차단)
            if (self.Status != null && !self.Status.CanUseDamageType(DamageType.Physical)) return;

            basicAttackStrategy.PerformAttack(self, Intent.Target);
        }

        internal bool TryGetRequestedSkill(out SkillDefinitionSO skill)
        {
            skill = Intent.RequestedSkill;
            if (skill == null) return false;
            if (!HasValidTarget()) return false;

            // ✅ 스킬 시전 불가(침묵/스턴 등)
            if (self.Status != null && !self.Status.CanCastSkill()) return false;

            // ✅ 해당 타입 공격 불가(마법/총기/물리 차단)
            if (self.Status != null && !self.Status.CanUseDamageType(skill.damageType)) return false;

            // ✅ 캐스터 태그 제한
            if (!skill.CanBeUsedBy(self)) return false;

            // ✅ 쿨타임
            if (!self.IsSkillReady(skill)) return false;

            // ✅ 사거리(0이면 제한 없음으로 해석)
            if (skill.range > 0f && DistanceToTarget() > skill.range) return false;

            return true;
        }

        internal void ExecuteSkill(SkillDefinitionSO skill)
        {
            if (skill == null) return;
            if (!HasValidTarget()) return;

            skillExecutor.Execute(self, Intent.Target, skill);
            self.ConsumeSkillCooldown(skill);
        }
    }
}
