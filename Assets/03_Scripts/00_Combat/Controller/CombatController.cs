using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using MyGame.Application.Tick;
using MyGame.Application;
using MyGame.Party;

namespace MyGame.Combat
{
    public partial class CombatController : MonoBehaviour, ISimulationTickable
    {
        [Header("Wiring")]
        [SerializeField] private Actor self;
        [SerializeField] private MonoBehaviour brainComponent; // ICombatBrain 구현체
        [SerializeField] private ActorAnimatorDriver animDriver;
        [SerializeField] private SpawnAreaBox respawnArea;

        private ICombatBrain brain;

        [Header("Strategies")]
        public IBasicAttackStrategy basicAttackStrategy = new MeleeBasicAttackStrategy();
        public ISkillSelectorStrategy autoSkillSelector = new FirstReadySkillSelector();
        public ISkillExecutorStrategy skillExecutor = new InstantDamageSkillExecutor();

        private float manualBlockUntilUnscaled = 0f;
        public bool IsManualBlocked => Time.unscaledTime < manualBlockUntilUnscaled;

        // =========================================================
        // ✅ Formation 등 외부 명령으로 전투를 "잠깐 끊는" 플래그
        // =========================================================
        private float _suspendCombatUntilUnscaled = -1f;
        public bool IsCombatSuspended => Time.unscaledTime < _suspendCombatUntilUnscaled;

        /// <summary>
        /// ✅ 일정 시간 동안 전투(추적/공격)를 중단한다.
        /// - 이동 자체는 MoveInputResolver.ForcedMove로 따로 밀어줄 수 있다(형태변환 이동 등).
        /// </summary>
        public void SuspendCombatFor(float secondsUnscaled)
        {
            if (secondsUnscaled <= 0f) return;
            _suspendCombatUntilUnscaled = Mathf.Max(_suspendCombatUntilUnscaled, Time.unscaledTime + secondsUnscaled);

            // 즉시 전투 끊기(이 프레임부터 추적/공격이 밀어넣는 AutoMoveVector 영향 최소화)
            StopMove();
            // FSM이 다른 상태여도 Idle로 정리
            if (fsm != null) fsm.Change(CombatStateId.Idle);
        }

        public void ClearCombatSuspension()
        {
            _suspendCombatUntilUnscaled = -1f;
        }

        [SerializeField] private AutoModeController autoMode; // (글로벌) 컨트롤 중인 플레이어만 영향
        [SerializeField] private PartyControlRouter partyControl;
        private IMover mover;

        // ✅ (추가) 플레이어면 공격 직전 바라보기 요청용
        private global::PlayerMover playerMover;

        // ✅ (추가) 바라보기 유지 시간(플레이어에서 RequestFaceTarget duration으로 전달)
        [SerializeField] private float preAttackFaceDuration = 0.15f;

        private CombatStateMachine fsm;
        private float basicAttackCooldown = 0f;
        private Actor lastCooldownTarget = null;

        internal bool IsBasicAttackReady => basicAttackCooldown <= 0f;
        internal CombatIntent Intent { get; private set; }
        internal Actor Self => self;
        internal ActorAnimatorDriver Anim => animDriver;
        internal bool TryGetRequestedSkill(out SkillDefinitionSO skill) { skill = null; return false; }
        internal void ExecuteSkill(SkillDefinitionSO skill) { }

        private void Reset()
        {
            self = GetComponent<Actor>();
            if (animDriver == null) animDriver = GetComponent<ActorAnimatorDriver>();
        }

        private void Awake()
        {
            self = GetComponent<Actor>();
            mover = GetComponent<IMover>();

            // ✅ (추가) PlayerMover 캐싱 (몬스터에는 없을 수 있으니 null OK)
            playerMover = GetComponent<global::PlayerMover>();

            if (partyControl == null) partyControl = FindObjectOfType<PartyControlRouter>();

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

        /// <summary>
        /// 글로벌 AutoMode(ON/OFF)를 이 CombatController가 "적용받아야 하는지" 판단.
        /// - 파티 컨트롤이 없으면(단일 플레이어 씬) 기존 동작 유지: 플레이어는 AutoMode 영향 받음
        /// - 파티 컨트롤이 있으면: "현재 컨트롤 중인 플레이어"만 AutoMode 영향 받음
        ///   (컨트롤 중이 아닌 파티원은 AutoMode OFF여도 자동전투 지속)
        /// </summary>
        private bool ShouldHonorGlobalAutoMode()
        {
            if (self == null) return false;
            if (self.kind != ActorKind.Player) return false;
            if (autoMode == null) return false;

            if (partyControl == null) return true; // 기존 동작
            return partyControl.IsControlled(self);
        }

        private void OnEnable()
        {
            App.RegisterWhenReady(this);
        }

        private void OnDisable()
        {
            App.UnregisterTickable(this);
        }

        public void SimulationTick(float dt)
        {
            if (self == null) return;

            // 1) 죽었으면 Dead로
            if (!self.IsAlive)
            {
                Anim?.SetInCombat(false);
                fsm.Change(CombatStateId.Dead);
                fsm.Tick(dt);
                return;
            }

            // ✅ (최우선) Formation 등 외부 명령으로 전투를 잠깐 끊는 경우
            if (IsCombatSuspended)
            {
                Anim?.SetInCombat(false);
                Intent = CombatIntent.None;
                StopMove();

                // 공격/추적 상태에서 들어와도 안정적으로 Idle 유지
                fsm.Change(CombatStateId.Idle);
                fsm.Tick(dt);
                return;
            }

            // 2) Brain이 의사결정
            Intent = (brain != null) ? brain.Decide(self) : CombatIntent.None;

            bool inCombat = Intent.Engage && Intent.Target != null && Intent.Target.IsAlive;
            Anim?.SetInCombat(inCombat);

            // 3) 몬스터 자동 스킬 선택
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

            // 4) 일반공격 로직사이클
            UpdateBasicAttackCooldown(dt);

            // 5) CC상태(스턴 등) 처리: forced를 FSM에 반영
            if (self.Status != null && self.Status.TryGetForcedState(out var forced))
            {
                if (forced != CombatStateId.Dead && forced != CombatStateId.Respawn)
                {
                    fsm.Change(forced);
                    fsm.Tick(dt);
                    return;
                }
            }

            // 6) (플레이어만) Auto OFF면 자동전투 멈춤
            //    ✅ 파티 시스템: "컨트롤 중인 플레이어"만 글로벌 AutoMode의 영향을 받는다.
            if (ShouldHonorGlobalAutoMode() && !autoMode.IsAuto)
            {
                Anim?.SetInCombat(false);
                Intent = CombatIntent.None;
                StopMove();
                fsm.Change(CombatStateId.Idle);
                fsm.Tick(dt);
                return;
            }

            // 공격 애니 트리거보다 먼저 공격 직전 방향조정 요청
            TryFaceTargetBeforeBasicAttack();

            // 7) 상태머신 진행
            fsm.Tick(dt);
        }

        // 공격이 나갈 타이밍에 바라보게 하는 기능
        private void TryFaceTargetBeforeBasicAttack()
        {
            // 플레이어가 아니면 스킵
            if (playerMover == null) return;

            // 전투 중 + 타겟 유효
            if (!Intent.Engage) return;
            if (!HasValidTarget()) return;

            // 사거리 안일 때만 (멀리 있는 타겟으로 고개 도는 문제 방지)
            if (!IsInAttackRange()) return;

            // 이번 프레임에 기본공격이 "나갈 수 있는 상태"일 때만
            if (!IsBasicAttackReady) return;
            if (!CanBasicAttackNow()) return;

            // 여기서 요청(즉시 회전)
            playerMover.RequestFaceTarget(Intent.Target, preAttackFaceDuration, immediate: true);
        }

        public void BlockAutoCombatFor(float seconds)
        {
            if (seconds <= 0f) return;

            manualBlockUntilUnscaled = Mathf.Max(manualBlockUntilUnscaled, Time.unscaledTime + seconds);

            if (ShouldHonorGlobalAutoMode() && autoMode.IsAuto)
                autoMode.SetAuto(false);
        }

        private void UpdateBasicAttackCooldown(float dt)
        {
            var t = Intent.Target;

            if (!Intent.Engage || t == null || !t.IsAlive)
            {
                basicAttackCooldown = 0f;
                lastCooldownTarget = null;
                return;
            }

            if (t != lastCooldownTarget)
            {
                lastCooldownTarget = t;
                basicAttackCooldown = 0f;
                return;
            }

            basicAttackCooldown = Mathf.Max(0f, basicAttackCooldown - dt);
        }

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

            if (!IsBasicAttackReady) return;

            if (self.Status != null && !self.Status.CanBasicAttack()) return;
            if (self.Status != null && !self.Status.CanUseDamageType(DamageType.Physical)) return;

            // 공격 실행
            basicAttackStrategy.PerformAttack(self, Intent.Target);

            //  공격이 실제로 수행됐으면 쿨다운 시작
            StartBasicAttackCooldown();
        }

        internal void StartBasicAttackCooldown()
        {
            float rule = self.GetAttackInterval();
            float visual = (animDriver != null) ? animDriver.GetMinAttackSpacing() : 0f;

            basicAttackCooldown = Mathf.Max(rule, visual);
        }

        internal bool CanBasicAttackNow()
        {
            if (!HasValidTarget()) return false;

            if (!IsBasicAttackReady) return false;

            if (self.Status != null && !self.Status.CanBasicAttack()) return false;
            if (self.Status != null && !self.Status.CanUseDamageType(DamageType.Physical)) return false;

            return true;
        }
    }
}
