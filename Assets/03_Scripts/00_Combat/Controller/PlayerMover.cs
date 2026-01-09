using UnityEngine;
using MyGame.Combat;

public class PlayerMover : MonoBehaviour, IMover
{
    [SerializeField] private MoveInputResolver input;
    [SerializeField] private float speed = 5f;

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

        Vector3 move = input.GetMoveVector(); // 조이스틱 우선

        transform.position += move * (speed * Time.deltaTime);
    }
}
