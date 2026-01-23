using UnityEngine;

/// <summary>
/// 진행방향쪽으로 카메라를 당겨보게 만드는 기능을 수행
/// </summary>
public sealed class LookAheadFollowProxy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Look-ahead (world units)")]
    [SerializeField] private float lookAheadDistance = 1.5f;   // 기본 앞서가는 거리
    [SerializeField] private float maxLookAhead = 3.0f;        // 최대 앞서가기 제한
    [SerializeField] private float minSpeed = 0.2f;            // 이 속도 이하면 룩어헤드 0으로

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.08f; // 프록시 위치 스무딩
    [SerializeField] private float lookSmoothTime = 0.12f;     // 룩어헤드 벡터 스무딩
    [SerializeField] private bool ignoreY = true;              // 쿼터뷰면 보통 true

    private Vector3 lastTargetPos;
    private Vector3 posVel;
    private Vector3 lookVel;
    private Vector3 lookCurrent;

    // ✅ Director가 "타겟이 풀렸는지" 확인할 수 있게 공개 (Target None로 풀리는 문제 복구용)
    public Transform Target => target;
    public bool HasTarget => target != null;

    public void SetTarget(Transform t, bool snap = false)
    {
        target = t;
        if (target == null) return;

        lastTargetPos = target.position;
        lookCurrent = Vector3.zero;
        posVel = Vector3.zero;
        lookVel = Vector3.zero;

        if (snap)
            transform.position = target.position;
    }

    private void OnEnable()
    {
        if (target == null) return;

        // 재활성화 시 내부 상태 리셋(튀는 것 방지)
        lastTargetPos = target.position;
        lookCurrent = Vector3.zero;
        posVel = Vector3.zero;
        lookVel = Vector3.zero;

        transform.position = target.position;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 targetPos = target.position;
        Vector3 velocity = (targetPos - lastTargetPos) / dt;

        if (ignoreY) velocity.y = 0f;

        float speed = velocity.magnitude;

        Vector3 desiredLook = Vector3.zero;
        if (speed >= minSpeed)
        {
            desiredLook = velocity.normalized * Mathf.Min(maxLookAhead, lookAheadDistance);
        }

        lookCurrent = Vector3.SmoothDamp(lookCurrent, desiredLook, ref lookVel, lookSmoothTime);

        Vector3 desiredPos = targetPos + lookCurrent;
        if (ignoreY) desiredPos.y = targetPos.y;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVel, positionSmoothTime);

        lastTargetPos = targetPos;
    }
}
