using UnityEngine;

public class MoveInputResolver : MonoBehaviour
{
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private AutoModeController autoMode;

    [Header("Tuning")]
    [SerializeField] private float joystickDeadZone = 0.05f; // 0.05~0.12 사이에서 조정

    public Vector3 AutoMoveVector { get; set; }

    public Vector3 GetMoveVector()
    {
        Vector2 j = (joystick != null) ? joystick.InputVector : Vector2.zero;

        // 1) 조이스틱 입력이 "데드존 이상"이면 최우선
        if (j.sqrMagnitude > joystickDeadZone * joystickDeadZone)
            return new Vector3(j.x, 0f, j.y);

        // 2) 입력이 없고 Auto ON이면 Auto 벡터
        if (autoMode != null && autoMode.IsAuto)
            return AutoMoveVector;

        // 3) 나머지는 정지
        return Vector3.zero;
    }
}
