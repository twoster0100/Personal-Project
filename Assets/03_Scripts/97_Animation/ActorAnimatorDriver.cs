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
        [Tooltip("PlayerMover.speed 와 맞추는 값(최대 이동속도)")]
        [SerializeField] private float maxMoveSpeed = 5f;
        [SerializeField] private float speedDamp = 0.10f;

        [Header("Idle(Stay)")]
        [SerializeField] private float idleEnterSpeedThreshold = 0.05f;

        [Header("Animator Params")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string idleTimeParam = "IdleTime";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string castTrigger = "Cast";
        [SerializeField] private string isDeadBool = "IsDead";

        private int _hashSpeed;
        private int _hashIdleTime;
        private int _hashAttack;
        private int _hashCast;
        private int _hashIsDead;

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

            _hashSpeed = Animator.StringToHash(speedParam);
            _hashIdleTime = Animator.StringToHash(idleTimeParam);
            _hashAttack = Animator.StringToHash(attackTrigger);
            _hashCast = Animator.StringToHash(castTrigger);
            _hashIsDead = Animator.StringToHash(isDeadBool);

            _prevPos = transform.position;
        }

        /// <summary>플레이어 조이스틱/오토 여부 전달용 (IdleTime 계산에 사용)</summary>
        public void SetIsAuto(bool isAuto) => _isAuto = isAuto;

        private void LateUpdate()
        {
            if (animator == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1) Speed: 실제 이동량 기반
            var delta = transform.position - _prevPos;
            _prevPos = transform.position;

            float metersPerSec = delta.magnitude / dt;
            float speed01 = (maxMoveSpeed <= 0f) ? 0f : Mathf.Clamp01(metersPerSec / maxMoveSpeed);

            animator.SetFloat(_hashSpeed, speed01, speedDamp, dt);

            // 2) IdleTime: 오토면 0, 수동이고 거의 안 움직이면 누적
            if (_isAuto)
            {
                _idleTime = 0f;
            }
            else
            {
                if (speed01 > idleEnterSpeedThreshold) _idleTime = 0f;
                else _idleTime += dt;
            }

            animator.SetFloat(_hashIdleTime, _idleTime);

            // 3) Dead bool
            if (self != null)
                animator.SetBool(_hashIsDead, !self.IsAlive);
        }

        public void TriggerAttack()
        {
            if (animator != null) animator.SetTrigger(_hashAttack);
        }

        public void TriggerCast()
        {
            if (animator != null) animator.SetTrigger(_hashCast);
        }
    }
}
