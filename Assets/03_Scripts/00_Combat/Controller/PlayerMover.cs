using UnityEngine;
using MyGame.Combat;

public class PlayerMover : MonoBehaviour, IMover
{
    [SerializeField] private MoveInputResolver input;

    // 기존 유지(혹시 Actor가 없거나 별도 튜닝용)
    [SerializeField] private float speed = 5f;

    // Actor가 있으면 Actor.walkSpeed를 우선 사용(추천)
    [SerializeField] private bool useActorWalkSpeed = true;

    private Actor actor;

    private void Awake()
    {
        actor = GetComponent<Actor>();
    }

    // CombatController가 "자동 이동 의도"를 줄 때 호출
    public void SetDesiredMove(Vector3 worldMoveDir01)
    {
        if (input == null) return;

        worldMoveDir01.y = 0f;

        if (worldMoveDir01.sqrMagnitude < 0.0001f)
            input.AutoMoveVector = Vector3.zero;
        else
            input.AutoMoveVector = worldMoveDir01.normalized; // Auto는 방향만 쓰는 게 보통 안정적
    }

    public void Stop()
    {
        if (input == null) return;
        input.AutoMoveVector = Vector3.zero;
    }

    // ✅ LateUpdate로 이동 적용(CombatController의 Update 이후에 움직이게)
    private void LateUpdate()
    {
        if (input == null) return;

        // 이동불가(스턴/루트 등)면 정지
        if (actor != null && actor.Status != null && !actor.Status.CanMove())
            return;

        Vector3 move = input.GetMoveVector(); // (조이스틱 우선) or (Auto ON이면 AutoMoveVector)
        if (move == Vector3.zero) return;

        float dt = Time.deltaTime;

        float finalSpeed =
            (useActorWalkSpeed && actor != null) ? actor.walkSpeed : speed;

        transform.position += move * (finalSpeed * dt);
    }
}
