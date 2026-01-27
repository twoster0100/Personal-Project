using UnityEngine;
using MyGame.Combat;
using MyGame.Application.Tick;
using MyGame.Application;

[RequireComponent(typeof(MyGame.Combat.Actor))]
public class MonsterMover : MonoBehaviour, IMover, IFrameTickable
{
    private MyGame.Combat.Actor actor;
    private Vector3 desiredDir01;

    private void Awake()
    {
        actor = GetComponent<MyGame.Combat.Actor>();
    }

    private void OnEnable()
    {
        if (global::UnityEngine.Application.isPlaying)
            App.RegisterWhenReady(this);
    }

    private void OnDisable()
    {
        App.UnregisterTickable(this);
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

    public void FrameTick(float dt)
    {
        if (dt <= 0f) return;
        if (desiredDir01 == Vector3.zero) return;

        transform.position += desiredDir01 * (actor.walkSpeed * dt);
    }
}
