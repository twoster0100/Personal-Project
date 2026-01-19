using UnityEngine;

namespace MyGame.Combat
{
    [DisallowMultipleComponent]
    public class ActorAnimatorDriver : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Animator animator;   // Model 쪽 Animator
        [SerializeField] private Actor self;          // BunnyRoot_0의 Actor

        [Header("Speed")]
        [SerializeField] private float maxMoveSpeed = 5f;   // PlayerMover의 speed와 맞추기
        [SerializeField] private float speedDamp = 0.10f;

        [Header("Animator Params")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string castTrigger = "Cast";
        [SerializeField] private string isDeadBool = "IsDead";

        private int _hashSpeed, _hashAttack, _hashCast, _hashIsDead;
        private Vector3 _prevPos;

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
            _hashAttack = Animator.StringToHash(attackTrigger);
            _hashCast = Animator.StringToHash(castTrigger);
            _hashIsDead = Animator.StringToHash(isDeadBool);

            _prevPos = transform.position;
        }

        private void LateUpdate()
        {
            if (animator == null) return;

            // 이동 속도(실제 이동량 기반) -> Speed(0~1)
            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                var delta = (transform.position - _prevPos);
                _prevPos = transform.position;

                float metersPerSec = delta.magnitude / dt;
                float speed01 = (maxMoveSpeed <= 0f) ? 0f : Mathf.Clamp01(metersPerSec / maxMoveSpeed);

                animator.SetFloat(_hashSpeed, speed01, speedDamp, dt);
            }

            // 죽음 상태(bool) , 리스폰 대비
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
