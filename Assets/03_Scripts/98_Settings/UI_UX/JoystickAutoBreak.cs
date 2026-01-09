using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickAutoBreak : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private AutoModeController autoMode;

    [SerializeField] private float breakThreshold = 0.5f; // 입력 크기가 이 이상이면 Auto OFF

    public void OnPointerDown(PointerEventData eventData)
    {
        // 손가락이 닿는 순간 “수동 의도”로 보고 Auto OFF
        if (autoMode.IsAuto) autoMode.SetAuto(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 아주 미세한 터치 오작동 방지용
        if (autoMode.IsAuto && joystick.Magnitude >= breakThreshold)
            autoMode.SetAuto(false);
    }
}
