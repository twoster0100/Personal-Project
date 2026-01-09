using UnityEngine;

public class MoveInputResolver : MonoBehaviour
{
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private AutoModeController autoMode;

    public Vector3 AutoMoveVector { get; set; }

    public Vector3 GetMoveVector()
    {
        // 1) 조이스틱 입력이 있으면 그게 최우선
        Vector2 j = joystick.InputVector;
        if (j != Vector2.zero)
            return new Vector3(j.x, 0f, j.y);

        // 2) 조이스틱 입력이 없고 Auto ON이면 Auto 벡터 사용
        if (autoMode.IsAuto)
            return AutoMoveVector;

        // 3) 아무것도 없으면 정지
        return Vector3.zero;
    }
}
