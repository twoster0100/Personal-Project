using UnityEngine;
using MyGame.Combat;

[RequireComponent(typeof(MyGame.Combat.Actor))]
public class MonsterMover : MonoBehaviour, IMover
{
    private MyGame.Combat.Actor actor;
    private Vector3 desiredDir01;

    private void Awake()
    {
        actor = GetComponent<MyGame.Combat.Actor>();
    }

    public void SetDesiredMove(Vector3 worldDir01)
    {
        worldDir01.y = 0f;
        desiredDir01 = (worldDir01.sqrMagnitude < 0.0001f) ? Vector3.zero : worldDir01.normalized;
    }

    public void Stop()
    {
        desiredDir01 = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (desiredDir01 == Vector3.zero) return;
        transform.position += desiredDir01 * (actor.walkSpeed * Time.deltaTime);
    }
}
