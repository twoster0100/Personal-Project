using UnityEngine;
using MyGame.Combat;

public class PlayerMover : MonoBehaviour, IMover
{
    [SerializeField] private MoveInputResolver input;
    [SerializeField] private float speed = 5f;
    [SerializeField] private Actor self;

    [Header("Animation Driver")]
    [SerializeField] private ActorAnimatorDriver animDriver;

    private void Reset()
    {
        if (input == null) input = GetComponent<MoveInputResolver>();
        if (self == null) self = GetComponent<Actor>();
        if (animDriver == null) animDriver = GetComponent<ActorAnimatorDriver>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<MoveInputResolver>();
        if (self == null) self = GetComponent<Actor>();
        if (animDriver == null) animDriver = GetComponent<ActorAnimatorDriver>();
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

    private void Update()
    {
        if (input == null) return;

        // AnimDriver에 오토 여부 전달(IdleTime 계산용)
        animDriver?.SetIsAuto(input.IsAuto);

        // 이동 불가 조건
        if (self != null && !self.IsAlive)
        {
            Stop();
            return;
        }

        if (self != null && self.Status != null && !self.Status.CanMove())
        {
            Stop();
            return;
        }

        Vector3 move = input.GetMoveVector();
        transform.position += move * (speed * Time.deltaTime);
    }
}
