using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace MyGame.Combat
{
   /// <summary>
    /// ++ 공용 전투 실행기(FSM + Strategy)
    /// - Brain(Player/Monster)이 만든 CombatIntent를 받아서 "상태머신"을 진행
    /// - 공격/스킬 실행은 Strategy로 위임
    /// - StatusController(버프/디버프)가 행동 제한/강제상태(Stun)를 제어
    /// - 타겟 찾고 ,공격할지/추적할지 결정하고 ,이동 방향만 만들어냄
  /// </summary>
    public partial class CombatController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Actor self;
        [SerializeField] private MonoBehaviour brainComponent; // ICombatBrain 구현체

        [SerializeField] private ActorAnimatorDriver animDriver;
        private ICombatBrain brain;

        [Header("Strategies")]
        public IBasicAttackStrategy basicAttackStrategy = new MeleeBasicAttackStrategy();
        public ISkillSelectorStrategy autoSkillSelector = new FirstReadySkillSelector();
        public ISkillExecutorStrategy skillExecutor = new InstantDamageSkillExecutor();



        // 수동 입력이 들어오면 일정 시간 자동전투(추적/공격 Intent)를 완전 차단
        private float manualBlockUntilUnscaled = 0f;
        public bool IsManualBlocked => Time.unscaledTime < manualBlockUntilUnscaled;

        [SerializeField] private AutoModeController autoMode; // 플레이어만 연결
        private IMover mover;

        private CombatStateMachine fsm;

        internal CombatIntent Intent { get; private set; }
        internal Actor Self => self;
        internal ActorAnimatorDriver Anim => animDriver;



        private void Reset()
        {
            self = GetComponent<Actor>();
            if (animDriver == null) animDriver = GetComponent<ActorAnimatorDriver>();
        }

        private void Awake()
        {
            self = GetComponent<Actor>();
            mover = GetComponent<IMover>();

            if (animDriver == null) animDriver = GetComponent<ActorAnimatorDriver>();

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

            float dt = Time.deltaTime;

            // 1) 죽었으면 Dead로
            if (!self.IsAlive)
            {
                fsm.Change(CombatStateId.Dead);
                fsm.Tick(dt);
                return;
            }

            // 2) Brain이 의사결정
            Intent = (brain != null) ? brain.Decide(self) : CombatIntent.None;

            // 3) (선택) 몬스터 자동 스킬 선택(Brain이 요청 안 하면 보충)
            if (Intent.Engage && Intent.Target != null && Intent.RequestedSkill == null && self.kind == ActorKind.Monster)
            {
                var picked = autoSkillSelector.SelectSkill(self, Intent.Target);
                if (picked != null)
                {
                    Intent = new CombatIntent
                    {
                        Target = Intent.Target,
                        Engage = Intent.Engage,
                        RequestedSkill = picked
                    };
                }
            }

            // 4) ✅ 강제 상태(스턴 등) 처리: forced를 "진짜로" FSM에 반영
            if (self.Status != null && self.Status.TryGetForcedState(out var forced))
            {
                if (forced != CombatStateId.Dead && forced != CombatStateId.Respawn)
                {
                    fsm.Change(forced);
                    fsm.Tick(dt);
                    return;
                }
            }

            //  플레이어: Auto OFF면 자동전투(이동/공격) 자체를 멈춤
            if (self.kind == ActorKind.Player && autoMode != null && !autoMode.IsAuto)
            {
                Intent = CombatIntent.None;
                StopMove();
                fsm.Change(CombatStateId.Idle);
                fsm.Tick(dt);
                return;
            }

            // 5) 상태머신 진행
            fsm.Tick(dt);
        }

        /// <summary>
        /// 수동 입력 발생 시 호출.
        /// seconds 동안 CombatIntent(추적/공격)를 무시하도록 차단.
        /// </summary>
        public void BlockAutoCombatFor(float seconds)
        {
            if (seconds <= 0f) return;

            manualBlockUntilUnscaled = Mathf.Max(manualBlockUntilUnscaled, Time.unscaledTime + seconds);

            if (autoMode != null && autoMode.IsAuto)
                autoMode.SetAuto(false);
        }

        // =========================
        // 상태들이 사용하는 유틸
        // =========================
        internal void StopMove() => mover?.Stop();
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
            if (mover == null) return;

            if (self.Status != null && !self.Status.CanMove()) { mover.Stop(); return; }
            if (!HasValidTarget()) { mover.Stop(); return; }

            Vector3 dir = Intent.Target.transform.position - self.transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude <= 0.001f) { mover.Stop(); return; }

            mover.SetDesiredMove(dir.normalized);
        }


        internal void DoBasicAttack()
        {
            if (!HasValidTarget()) return;

            //  기본공격불가
            if (self.Status != null && !self.Status.CanBasicAttack()) return;

            // 물리공격불가(Physical 차단)
            if (self.Status != null && !self.Status.CanUseDamageType(DamageType.Physical)) return;

            basicAttackStrategy.PerformAttack(self, Intent.Target);
        }

        internal bool TryGetRequestedSkill(out SkillDefinitionSO skill) { skill = null; return false; }
        internal void ExecuteSkill(SkillDefinitionSO skill) { }
    }
}
