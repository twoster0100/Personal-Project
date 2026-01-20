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
        [SerializeField] private float maxMoveSpeed = 5f;
        [SerializeField] private float speedDamp = 0.10f;
        [SerializeField] private float idleEnterSpeedThreshold = 0.05f;

        [Header("Animator Params")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string idleTimeParam = "IdleTime";
        [SerializeField] private string isDeadBool = "IsDead";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string castTrigger = "Cast";

        private int _hSpeed, _hIdleTime, _hIsDead, _hAttack, _hCast;

        private Vector3 _prevPos;
        private float _idleTime;
        private bool _isAuto;

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

            _prevPos = transform.position;
        }

        public void SetIsAuto(bool isAuto) => _isAuto = isAuto;

        private void Update()
        {
            if (animator == null || self == null) return;

            bool dead = !self.IsAlive;
            animator.SetBool(_hIsDead, dead);

            // Dead 중엔 파라미터/트리거 정리
            if (dead)
            {
                animator.ResetTrigger(_hAttack);
                animator.ResetTrigger(_hCast);
                animator.SetFloat(_hSpeed, 0f);
                animator.SetFloat(_hIdleTime, 0f);
            }
        }

        private void LateUpdate()
        {
            if (animator == null || self == null) return;
            if (!self.IsAlive) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 실제 이동량 기반 Speed
            var delta = transform.position - _prevPos;
            _prevPos = transform.position;

            float metersPerSec = delta.magnitude / dt;
            float speed01 = (maxMoveSpeed <= 0f) ? 0f : Mathf.Clamp01(metersPerSec / maxMoveSpeed);
            animator.SetFloat(_hSpeed, speed01, speedDamp, dt);

            // IdleTime(Stay)
            if (_isAuto) _idleTime = 0f;
            else
            {
                if (speed01 > idleEnterSpeedThreshold) _idleTime = 0f;
                else _idleTime += dt;
            }

            animator.SetFloat(_hIdleTime, _idleTime);
        }

        public void TriggerAttack()
        {
            if (animator != null) animator.SetTrigger(_hAttack);
        }

        public void TriggerCast()
        {
            if (animator != null) animator.SetTrigger(_hCast);
        }
    }
}
