using UnityEngine;

namespace MyGame.Combat
{
    [DisallowMultipleComponent]
    public class ActorAnimatorDriver : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Animator animator;
        [SerializeField] private Actor self;

        [Header("Locomotion")]
        [SerializeField] private float maxMoveSpeed = 5f;     // PlayerMover speed와 맞추기
        [SerializeField] private float speedDamp = 0.10f;
        [SerializeField] private float idleEnterSpeedThreshold = 0.05f;

        [Header("Stay Detect (optional)")]
        [SerializeField] private string stayStateTag = "Stay";
        [SerializeField] private string stayStateName = "Ani_Rabbit_Stay";
        [SerializeField] private string inCombatBool = "InCombat";

        [Header("Attack spacing (optional)")]
        [SerializeField] private AnimationClip attackClip;
        [SerializeField] private float attackStateSpeed = 3f;
        [SerializeField] private float attackClipLengthFallback = 0.5f;
        [SerializeField] private float attackSpacingPadding = 0.05f;

        [Header("Animator Params")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string idleTimeParam = "IdleTime";
        [SerializeField] private string isDeadBool = "IsDead";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string castTrigger = "Cast";

        private int _hSpeed, _hIdleTime, _hIsDead, _hAttack, _hCast, _hInCombat;
        private Vector3 _prevPos;
        private float _idleTime;

        // PlayerMover가 호출해도 깨지지 않게 유지
        private bool _isAuto;

        private bool _inCombat;
        public void SetInCombat(bool v) => _inCombat = v;
        public void SetIsAuto(bool isAuto) => _isAuto = isAuto;

        private void Reset()
        {
            if (self == null) self = GetComponent<Actor>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void Awake()
        {
            if (self == null) self = GetComponent<Actor>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);

            _hSpeed = Animator.StringToHash(speedParam);
            _hIdleTime = Animator.StringToHash(idleTimeParam);
            _hIsDead = Animator.StringToHash(isDeadBool);
            _hAttack = Animator.StringToHash(attackTrigger);
            _hCast = Animator.StringToHash(castTrigger);
            _hInCombat = Animator.StringToHash(inCombatBool);

            _prevPos = transform.position;
        }

        private void Update()
        {
            if (animator == null || self == null) return;

            bool dead = !self.IsAlive;

            animator.SetBool(_hIsDead, dead);
            animator.SetBool(_hInCombat, _inCombat && !dead);

            // Dead 중엔 트리거/로코모션/IdleTime 정리
            if (dead)
            {
                animator.ResetTrigger(_hAttack);
                animator.ResetTrigger(_hCast);
                animator.SetFloat(_hSpeed, 0f);
                animator.SetFloat(_hIdleTime, 0f);
                _idleTime = 0f;
                return;
            }
        }

        private void LateUpdate()
        {
            if (animator == null || self == null) return;
            if (!self.IsAlive) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1) Speed(실제 이동량 기반)
            Vector3 delta = transform.position - _prevPos;
            _prevPos = transform.position;

            float metersPerSec = delta.magnitude / dt;
            float speed01 = (maxMoveSpeed <= 0f) ? 0f : Mathf.Clamp01(metersPerSec / maxMoveSpeed);
            animator.SetFloat(_hSpeed, speed01, speedDamp, dt);

            // 2) Stay 상태면 IdleTime 0 고정 (연속 Stay/재진입 방지)
            if (IsStayState())
            {
                _idleTime = 0f;
                animator.SetFloat(_hIdleTime, 0f);
                return;
            }

            // 3) 전투 중이면 IdleTime 0 고정
            if (_inCombat)
            {
                _idleTime = 0f;
                animator.SetFloat(_hIdleTime, 0f);
                return;
            }

            // 4) IdleTime: 정지면 증가 / 움직이면 리셋
            if (speed01 > idleEnterSpeedThreshold) _idleTime = 0f;
            else _idleTime += dt;

            animator.SetFloat(_hIdleTime, _idleTime);
        }

        private bool IsStayState()
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);

            if (!string.IsNullOrEmpty(stayStateTag) && st.IsTag(stayStateTag))
                return true;

            if (!string.IsNullOrEmpty(stayStateName))
            {
                if (st.IsName(stayStateName)) return true;
                if (st.IsName("Base Layer." + stayStateName)) return true;
            }

            return false;
        }

        public void TriggerAttack()
        {
            if (animator != null) animator.SetTrigger(_hAttack);
        }

        public void TriggerCast()
        {
            if (animator != null) animator.SetTrigger(_hCast);
        }

        public float GetMinAttackSpacing()
        {
            float clipLen = (attackClip != null) ? attackClip.length : attackClipLengthFallback;
            float duration = clipLen / Mathf.Max(0.01f, attackStateSpeed);
            return Mathf.Max(0f, duration - attackSpacingPadding);
        }
    }
}
