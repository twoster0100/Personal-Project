using UnityEngine;
using MyGame.Application.Tick;
using MyGame.Composition;

public sealed class LookAheadFollowProxy : MonoBehaviour, ILateFrameTickable
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Look-ahead (world units)")]
    [SerializeField] private float lookAheadDistance = 1.5f;
    [SerializeField] private float maxLookAhead = 3.0f;
    [SerializeField] private float minSpeed = 0.2f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float lookSmoothTime = 0.12f;
    [SerializeField] private bool ignoreY = true;

    private Vector3 lastTargetPos;
    private Vector3 posVel;
    private Vector3 lookVel;
    private Vector3 lookCurrent;

    public Transform Target => target;
    public bool HasTarget => target != null;

    public void SetTarget(Transform t, bool snap = false)
    {
        target = t;
        if (target == null) return;

        ResetInternal(snap);
    }

    private void ResetInternal(bool snap)
    {
        lastTargetPos = target.position;
        lookCurrent = Vector3.zero;
        posVel = Vector3.zero;
        lookVel = Vector3.zero;

        if (snap)
            transform.position = target.position;
    }

    private void OnEnable()
    {
        if (target != null)
            ResetInternal(snap: true);

        // ✅ PlayMode에서만 등록 (EditMode 등록 방지)
        if (global::UnityEngine.Application.isPlaying)
            AppCompositionRoot.RegisterWhenReady(this);
    }

    private void OnDisable()
    {
        // ✅ OnDisable은 가드 없이 해제(도메인 리로드 OFF 환경에서도 안전)
        AppCompositionRoot.UnregisterTickable(this);
    }

    // ✅ LateUpdate 제거 → LateFrameTick
    public void LateFrameTick(float dt)
    {
        if (target == null) return;
        if (dt <= 0f) return;

        Vector3 targetPos = target.position;
        Vector3 velocity = (targetPos - lastTargetPos) / dt;
        if (ignoreY) velocity.y = 0f;

        float speed = velocity.magnitude;

        Vector3 desiredLook = Vector3.zero;
        if (speed >= minSpeed)
            desiredLook = velocity.normalized * Mathf.Min(maxLookAhead, lookAheadDistance);

        // ✅ dt를 명시하는 SmoothDamp 오버로드 사용(틱 기반으로 일관)
        lookCurrent = Vector3.SmoothDamp(
            lookCurrent, desiredLook, ref lookVel, lookSmoothTime, Mathf.Infinity, dt);

        Vector3 desiredPos = targetPos + lookCurrent;
        if (ignoreY) desiredPos.y = targetPos.y;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref posVel, positionSmoothTime, Mathf.Infinity, dt);

        lastTargetPos = targetPos;
    }
}
