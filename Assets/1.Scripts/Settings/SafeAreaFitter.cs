using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform _rect;
    Rect _lastSafeArea = new Rect(0, 0, 0, 0);
    ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;
    Vector2Int _lastResolution = new Vector2Int(0, 0);

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // 에디터/시뮬레이터에서 회전/해상도/세이프에어가 바뀔 수 있으니 감지해서 갱신
        if (Screen.safeArea != _lastSafeArea ||
            Screen.orientation != _lastOrientation ||
            Screen.width != _lastResolution.x ||
            Screen.height != _lastResolution.y)
        {
            ApplySafeArea();
        }
    }
    void ApplySafeArea()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();

        Rect sa = Screen.safeArea;

        Vector2 anchorMin = sa.position;
        Vector2 anchorMax = sa.position + sa.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rect.anchorMin = anchorMin;
        _rect.anchorMax = anchorMax;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;

        _lastSafeArea = sa;
        _lastOrientation = Screen.orientation;
        _lastResolution = new Vector2Int(Screen.width, Screen.height);
    }
}
