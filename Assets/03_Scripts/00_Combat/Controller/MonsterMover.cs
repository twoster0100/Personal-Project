using UnityEngine;
using MyGame.Combat;

[RequireComponent(typeof(Actor))]
public class MonsterMover : MonoBehaviour, IMover
{
    private Actor actor;
    private Vector3 desiredDir01 = Vector3.zero;

    private void Awake()
    {
        actor = GetComponent<Actor>();
    }

    public void SetDesiredMove(Vector3 worldMoveDir01)
    {
        worldMoveDir01.y = 0f;

        if (worldMoveDir01.sqrMagnitude < 0.0001f)
        {
            desiredDir01 = Vector3.zero;
            return;
        }

        desiredDir01 = worldMoveDir01.normalized; // 항상 정규화해서 저장
    }

    public void Stop()
    {
        desiredDir01 = Vector3.zero;
    }

    // LateUpdate: CombatController(Update)에서 의도 결정 후 "같은 프레임"에 이동 적용되게 함
    private void LateUpdate()
    {
        if (actor == null) return;

        // 이동불가(스턴/루트 등)면 정지
        if (actor.Status != null && !actor.Status.CanMove()) return;

        if (desiredDir01 == Vector3.zero) return;

        float dt = Time.deltaTime;
        transform.position += desiredDir01 * actor.walkSpeed * dt;
    }
}
