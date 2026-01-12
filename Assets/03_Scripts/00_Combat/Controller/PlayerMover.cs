using UnityEngine;
using MyGame.Combat;

public class PlayerMover : MonoBehaviour, IMover
{
    [SerializeField] private MoveInputResolver input;
    [SerializeField] private float speed = 5f;

    [SerializeField] private Actor self;

    private void Reset()
    {
        if (input == null) input = GetComponent<MoveInputResolver>();
        if (self == null) self = GetComponent<Actor>();
    }

    private void Awake()
    {
        if (self == null) self = GetComponent<Actor>();
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

        // 죽었거나(=리스폰 대기 포함) 이동 차단
        if (self != null && !self.IsAlive)
        {
            Stop();
            return;
        }
        // 이동 불가 상태(스턴/속박 등)면 수동 이동도 차단
        if (self != null && self.Status != null && !self.Status.CanMove())
        {
            Stop();
            return;
        }
        Vector3 move = input.GetMoveVector(); // 조이스틱 우선
        transform.position += move * (speed * Time.deltaTime);
    }
}
