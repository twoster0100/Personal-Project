using UnityEngine;
using MyGame.Combat;
using MyGame.Application.Tick;
using MyGame.Composition;

public class PlayerMover : MonoBehaviour, IMover, IFrameTickable
{
    [SerializeField] private MoveInputResolver input;
    [SerializeField] private float speed = 5f;
    [SerializeField] private Actor self;

    [Header("Rotate Target")]
    [SerializeField] private Transform rotateTarget;

    [Tooltip("0이면 즉시 회전. 값이 크면 더 빠르게 돔.")]
    [SerializeField] private float turnSpeed = 10f;

    [Tooltip("공격 시작 직전에만 타겟을 바라봄.")]
    [SerializeField] private bool faceTargetOnlyWhenRequested = true;

    [Tooltip("공격 직전 바라보기 유지 시간(초)")]
    [SerializeField] private float defaultFaceRequestDuration = 0.15f;

    [Tooltip("공격 시 바라보는 방향도 8방향(45도)으로 스냅할지")]
    [SerializeField] private bool snapFacingTo8WayOnRequest = true;

    [Tooltip("각도 기준 오프셋(도). 기본 0이면 +X가 0도 기준. 필요 시 -90 등 조정.")]
    [SerializeField] private float facingAngleOffsetDegrees = 0f;

    private Actor _faceRequestTarget;
    private float _faceRequestUntilTime;

    private void Reset()
    {
        if (input == null) input = GetComponent<MoveInputResolver>();
        if (self == null) self = GetComponent<Actor>();
        if (rotateTarget == null) rotateTarget = transform;
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<MoveInputResolver>();
        if (self == null) self = GetComponent<Actor>();
        if (rotateTarget == null) rotateTarget = transform;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        AppCompositionRoot.RegisterWhenReady(this);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        AppCompositionRoot.UnregisterTickable(this);
    }

    public void SetDesiredMove(Vector3 worldDir01)
    {
        if (input == null) return;
        worldDir01.y = 0f;
        input.AutoMoveVector = (worldDir01.sqrMagnitude < 0.0001f)
            ? Vector3.zero
            : worldDir01.normalized;
    }

    public void Stop()
    {
        if (input != null) input.AutoMoveVector = Vector3.zero;
    }

    public void RequestFaceTarget(Actor target, float duration = -1f, bool immediate = true)
    {
        if (target == null || !target.IsAlive) return;

        _faceRequestTarget = target;
        _faceRequestUntilTime = Time.time + (duration > 0f ? duration : defaultFaceRequestDuration);

        if (immediate)
        {
            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;

            if (snapFacingTo8WayOnRequest)
                to = QuantizeDirTo8Way(to, facingAngleOffsetDegrees);

            FaceWorldDirection(to, immediate: true, dt: 0f);
        }
    }

    public void ClearFaceRequest()
    {
        _faceRequestTarget = null;
        _faceRequestUntilTime = 0f;
    }

    // ✅ Update 제거 → Tick
    public void FrameTick(float dt)
    {
        if (input == null) return;

        if (self != null && !self.IsAlive) { Stop(); return; }
        if (self != null && self.Status != null && !self.Status.CanMove()) { Stop(); return; }

        Vector3 move = input.GetMoveVector();

        if (move.sqrMagnitude > 0.0001f)
        {
            FaceWorldDirection(move, immediate: false, dt: dt);
        }
        else
        {
            if (faceTargetOnlyWhenRequested &&
                _faceRequestTarget != null &&
                _faceRequestTarget.IsAlive &&
                Time.time <= _faceRequestUntilTime)
            {
                Vector3 to = _faceRequestTarget.transform.position - transform.position;
                to.y = 0f;

                if (snapFacingTo8WayOnRequest)
                    to = QuantizeDirTo8Way(to, facingAngleOffsetDegrees);

                FaceWorldDirection(to, immediate: false, dt: dt);
            }
        }

        transform.position += move * (speed * dt);
    }

    private void FaceWorldDirection(Vector3 worldDir, bool immediate, float dt)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(worldDir.normalized, Vector3.up);

        if (immediate || turnSpeed <= 0f || dt <= 0f)
            rotateTarget.rotation = targetRot;
        else
            rotateTarget.rotation = Quaternion.Slerp(rotateTarget.rotation, targetRot, turnSpeed * dt);
    }

    private static Vector3 QuantizeDirTo8Way(Vector3 worldDir, float angleOffsetDeg)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 0.0001f) return Vector3.zero;

        Vector3 n = worldDir.normalized;

        float angle = Mathf.Atan2(n.z, n.x) * Mathf.Rad2Deg;
        angle += angleOffsetDeg;
        angle = (angle % 360f + 360f) % 360f;

        int idx = Mathf.RoundToInt(angle / 45f) % 8;
        float snappedAngle = idx * 45f - angleOffsetDeg;
        float rad = snappedAngle * Mathf.Deg2Rad;

        return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
    }
}
