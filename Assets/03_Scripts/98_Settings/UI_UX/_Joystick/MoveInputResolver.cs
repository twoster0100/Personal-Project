using UnityEngine;
using MyGame.Combat;
using MyGame.Party;

public class MoveInputResolver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private AutoModeController autoMode;

    [Tooltip("현재 컨트롤 중인 파티 멤버를 판단하는 라우터(없으면 기존 동작).")]
    [SerializeField] private PartyControlRouter partyControl;

    [Tooltip("이 MoveInputResolver의 소유자(플레이어 Actor). 비워두면 같은 GameObject에서 자동 탐색.")]
    [SerializeField] private Actor owner;

    [Header("Tuning")]
    [SerializeField] private float joystickDeadZone = 0.05f;
    [SerializeField] private float hysteresisDegrees = 5f;
    [SerializeField] private float angleOffsetDegrees = 0f;
    [SerializeField] private bool force8Way = true;
    [SerializeField] private bool preserveMagnitude = true;

    /// <summary>
    /// 오토 이동 벡터(월드 XZ 평면). y는 항상 0으로 유지.
    /// CombatController/PlayerMover가 세팅한다.
    /// </summary>
    public Vector3 AutoMoveVector { get; set; }

    // =========================================================
    // ✅ Formation 등 "강제 이동" (최상위 우선순위)
    // =========================================================
    [Header("Forced Move (Highest Priority)")]
    [Tooltip("강제 이동 중에 조이스틱을 움직이면 강제 이동을 취소할지")]
    [SerializeField] private bool cancelForcedOnJoystick = false;

    private Vector3 _forcedMoveVector;
    private float _forcedUntilUnscaled = -1f;

    public bool HasForcedMove => Time.unscaledTime <= _forcedUntilUnscaled;

    /// <summary>
    /// ✅ 최상위 우선순위 강제 이동 벡터를 설정한다.
    /// - worldDir01: 월드 XZ 방향(정규화 권장). y는 무시된다.
    /// - durationUnscaled: unscaled 기준 유지 시간(초). 매 틱 갱신(연장)하는 방식으로 사용하면 안정적.
    /// </summary>
    public void SetForcedMove(Vector3 worldDir01, float durationUnscaled)
    {
        worldDir01.y = 0f;

        if (worldDir01.sqrMagnitude < 0.0001f)
            _forcedMoveVector = Vector3.zero;
        else
            _forcedMoveVector = worldDir01.normalized;

        float d = Mathf.Max(0f, durationUnscaled);
        _forcedUntilUnscaled = Time.unscaledTime + d;
    }

    public void ClearForcedMove()
    {
        _forcedMoveVector = Vector3.zero;
        _forcedUntilUnscaled = -1f;
    }

    private int _lastDirIndex = -1; // 0..7

    private void Awake()
    {
        if (owner == null) owner = GetComponent<Actor>();
        if (partyControl == null) partyControl = FindObjectOfType<PartyControlRouter>();
        if (joystick == null) joystick = FindObjectOfType<FixedJoystick>();
        if (autoMode == null) autoMode = FindObjectOfType<AutoModeController>();
    }

    private bool IsOwnerControlled()
    {
        if (partyControl == null) return true;
        if (owner == null) return true;
        return partyControl.IsControlled(owner);
    }

    /// <summary>
    /// ✅ 항상 월드 XZ 평면(Vector3(x,0,z))만 반환한다.
    /// </summary>
    public Vector3 GetMoveVector()
    {
        bool isControlled = IsOwnerControlled();

        // 조이스틱 입력은 cancelForcedOnJoystick 판단에도 쓰인다.
        Vector2 j = Vector2.zero;
        if (isControlled && joystick != null)
            j = joystick.InputVector;

        bool hasJoystick = j.sqrMagnitude >= (joystickDeadZone * joystickDeadZone);

        Vector2 raw2;

        // 0) ✅ ForcedMove (최상위)
        if (HasForcedMove)
        {
            if (cancelForcedOnJoystick && hasJoystick)
            {
                ClearForcedMove();
                // 아래 일반 로직으로 폴백
            }
            else
            {
                raw2 = new Vector2(_forcedMoveVector.x, _forcedMoveVector.z);
                if (force8Way) raw2 = Snap8Way(raw2);
                return new Vector3(raw2.x, 0f, raw2.y);
            }
        }

        // 1) 조이스틱 입력(컨트롤 중인 캐릭터만)
        if (hasJoystick)
        {
            raw2 = j; // (x, y) => (worldX, worldZ)
        }
        else
        {
            Vector2 auto2 = new Vector2(AutoMoveVector.x, AutoMoveVector.z);

            if (isControlled)
            {
                // 컨트롤 중인 캐릭터는 AutoMode가 켜져있을 때만 오토 이동
                raw2 = (autoMode != null && autoMode.IsAuto) ? auto2 : Vector2.zero;
            }
            else
            {
                // 컨트롤 중이 아닌 캐릭터는 항상 오토 이동(글로벌 AutoMode 영향 X)
                raw2 = auto2;
            }
        }

        // 2) 스냅/보정
        if (force8Way)
            raw2 = Snap8Way(raw2);

        // 3) Vector2(x,y)를 월드 XZ로 매핑 (y는 항상 0)
        return new Vector3(raw2.x, 0f, raw2.y);
    }

    private Vector2 Snap8Way(Vector2 v)
    {
        if (v == Vector2.zero)
        {
            _lastDirIndex = -1;
            return Vector2.zero;
        }

        float mag = preserveMagnitude ? Mathf.Clamp01(v.magnitude) : 1f;

        // angle 0 = +X axis. 0..360
        float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        ang = (ang + 360f + angleOffsetDegrees) % 360f;

        int idx = Mathf.RoundToInt(ang / 45f) % 8;

        // hysteresis
        if (_lastDirIndex >= 0 && hysteresisDegrees > 0f)
        {
            float center = _lastDirIndex * 45f;
            float delta = Mathf.DeltaAngle(ang, center);
            if (Mathf.Abs(delta) <= hysteresisDegrees)
                idx = _lastDirIndex;
        }

        _lastDirIndex = idx;

        float snappedAng = idx * 45f - angleOffsetDegrees;
        float rad = snappedAng * Mathf.Deg2Rad;

        Vector2 outDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        return outDir.normalized * mag;
    }
}
