using UnityEngine;
using MyGame.Combat;

public class PlayerMover : MonoBehaviour, IMover
{
    [SerializeField] private MoveInputResolver input;
    [SerializeField] private float speed = 5f;

    [SerializeField] private Actor self;


    [Header("Animation (quick test)")]
    [SerializeField] private Animator animator;
    private int _speedHash;
    private float idleTime;
    private int idleTimeHash;

    private void Reset()
    {
        if (input == null) input = GetComponent<MoveInputResolver>();
        if (self == null) self = GetComponent<Actor>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    private void Awake()
    {
        if (self == null) self = GetComponent<Actor>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        _speedHash = Animator.StringToHash("Speed");
        idleTimeHash = Animator.StringToHash("IdleTime");
    }

    public void SetDesiredMove(Vector3 worldDir01)
    {
        if (input == null) return;
        worldDir01.y = 0f;
        input.AutoMoveVector = (worldDir01.sqrMagnitude < 0.0001f) ? Vector3.zero : worldDir01.normalized;
    }

    public void Stop()
    {
        if (input != null) input.AutoMoveVector = Vector3.zero;
    }
    private void LateUpdate()
    {
        if (input == null) return;

        animator.SetBool("IsDead", self != null && !self.IsAlive);

        if (self != null && !self.IsAlive) { Stop(); SetSpeed(0f); idleTime = 0f; animator?.SetFloat(idleTimeHash, idleTime); return; }
        if (self != null && self.Status != null && !self.Status.CanMove()) { Stop(); SetSpeed(0f); idleTime = 0f; animator?.SetFloat(idleTimeHash, idleTime); return; }

        Vector3 move = input.GetMoveVector();

        transform.position += move * (speed * Time.deltaTime);

        float speed01 = Mathf.Clamp01(move.magnitude);

        if (input.IsAuto)
        {
            idleTime = 0f;
        }
        else
        {
            if (speed01 > 0.05f) idleTime = 0f;
            else idleTime += Time.deltaTime;
        }

        if (animator != null)
        {
            animator.SetFloat(idleTimeHash, idleTime);
        }

        SetSpeed(speed01);
    }


    private void SetSpeed(float v)
    {
        if (animator != null) animator.SetFloat(_speedHash, v);
    }
}
