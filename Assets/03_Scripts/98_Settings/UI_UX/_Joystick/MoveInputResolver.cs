using UnityEngine;

public class MoveInputResolver : MonoBehaviour
{
    /// <summary>
    /// 8방향(45도) 스냅. 조이스틱/오토 공통 적용.
    /// - preserveMagnitude=true면 입력 크기(0~1)를 유지해서 속도도 자연스럽게 변함
    /// - hysteresisDegrees>0이면 방향 경계에서 깜빡임(왔다갔다)을 줄임
    /// - angleOffsetDegrees로 0도 기준을 회전시킬 수 있음
    /// </summary>
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private AutoModeController autoMode;

    [Header("Tuning")]
    [SerializeField] private float joystickDeadZone = 0.05f; // 0.05~0.12 사이에서 조정
    [SerializeField] private float hysteresisDegrees = 5f;
    [SerializeField] private float angleOffsetDegrees = 0f;
    [SerializeField] private bool force8Way = true; // 8방향 이동
    [SerializeField] private bool preserveMagnitude = true; // 항상 1의 속도로 이동 방향만 고정(false) , 조이스틱 살살 밀면 천천히 걷는 느낌(ture)

    public bool IsAuto => autoMode != null && autoMode.IsAuto;

    //자동전투에서 들어오는 이동 벡터(월드 기준 XZ)
    public Vector3 AutoMoveVector { get; set; }

    // 히스테리시스용 상태
    private int _lastDirIndex = -1; // 0~7
    private bool _hadNonZeroLastFrame = false;

    public Vector3 GetMoveVector()
    {
        Vector2 j = (joystick != null) ? joystick.InputVector : Vector2.zero;

        // 1) 조이스틱 입력이 "데드존 이상"이면 최우선
        Vector3 raw;
        if (j.sqrMagnitude > joystickDeadZone * joystickDeadZone)
        {
            raw = new Vector3(j.x, 0f, j.y);
        }
        // 2) 입력이 없고 Auto ON이면 Auto 벡터
        else if (autoMode != null && autoMode.IsAuto)
        {
            raw = AutoMoveVector;
            raw.y = 0f;
        }
        // 3) 나머지는 정지
        else
        {
            raw = Vector3.zero;
        }

        if (!force8Way) return raw;

        return SnapTo8Way(raw, preserveMagnitude, hysteresisDegrees, angleOffsetDegrees);
    }


    private Vector3 SnapTo8Way(Vector3 v, bool preserveMag, float hysteresisDeg, float angleOffsetDeg)
    {
        v.y = 0f;
        float mag = v.magnitude;
        if (mag < 0.0001f)
        {
            _hadNonZeroLastFrame = false;
            return Vector3.zero;
        }

        Vector3 n = v / mag;

        // 기본: atan2(z, x) => +X가 0도, +Z가 90도
        float angle = Mathf.Atan2(n.z, n.x) * Mathf.Rad2Deg;
        angle += angleOffsetDeg;

        // 0~360로 정규화
        angle = (angle % 360f + 360f) % 360f;

        int nearest = Mathf.RoundToInt(angle / 45f) % 8;

        // 히스테리시스: 지난 방향이 있고, 아직 이동 중이면 경계 완충
        if (_hadNonZeroLastFrame && _lastDirIndex >= 0 && hysteresisDeg > 0f)
        {
            float center = _lastDirIndex * 45f;
            float delta = Mathf.Abs(Mathf.DeltaAngle(angle, center));

            // 기본 경계는 22.5도. 여기에 완충을 더함
            if (delta <= 22.5f + hysteresisDeg)
            {
                nearest = _lastDirIndex;
            }
        }

        _lastDirIndex = nearest;
        _hadNonZeroLastFrame = true;

        float snappedAngle = nearest * 45f - angleOffsetDeg; // 오프셋 되돌려서 실제 방향 벡터 생성
        float rad = snappedAngle * Mathf.Deg2Rad;

        Vector3 dir = new(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

        if (preserveMag)
            return dir * Mathf.Clamp01(mag);

        return dir; // 방향만 고정(항상 1)
    }
}
