using UnityEngine;
using UnityEngine.EventSystems;

public class FixedJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Refs")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private Canvas canvas;

    [Header("Tuning")]
    [SerializeField] private float radius = 90f;      // 핸들 최대 이동 반경(px)
    [SerializeField] private float deadZone = 0.15f;  // 0~1 (떨림 방지)

    public Vector2 InputVector { get; private set; }  // -1~1
    public float Magnitude => InputVector.magnitude;

    private int _activePointerId = int.MinValue;

    private void Reset()
    {
        background = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Awake()
    {
        if (!background) background = GetComponent<RectTransform>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        SetHandle(Vector2.zero);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 이미 다른 손가락이 잡고 있으면 무시
        if (_activePointerId != int.MinValue) return;

        _activePointerId = eventData.pointerId;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != _activePointerId) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out var localPos))
            return;

        var clamped = Vector2.ClampMagnitude(localPos, radius);
        handle.anchoredPosition = clamped;

        var raw = clamped / radius; // -1~1

        // 데드존 적용
        if (raw.magnitude < deadZone) raw = Vector2.zero;

        InputVector = raw;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != _activePointerId) return;

        _activePointerId = int.MinValue;
        SetHandle(Vector2.zero);
    }

    private void SetHandle(Vector2 normalized)
    {
        InputVector = normalized;
        handle.anchoredPosition = normalized * radius;
    }
}
