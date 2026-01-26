using UnityEngine;
using MyGame.Application.Tick;
using MyGame.Composition;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour, IUnscaledFrameTickable
{
    RectTransform _rect;
    Rect _lastSafeArea = new(0, 0, 0, 0);
    ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;
    Vector2Int _lastResolution = new(0, 0);

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void OnEnable()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        ApplySafeArea();

        // ✅ PlayMode에서만 Tick 등록 (에디터 모드 등록 방지)
        if (Application.isPlaying)
            AppCompositionRoot.RegisterWhenReady(this);
    }

    void OnDisable()
    {
        if (Application.isPlaying)
            AppCompositionRoot.UnregisterTickable(this);
    }

    // ✅ 런타임 Update 제거 → UnscaledFrameTick
    public void UnscaledFrameTick(float unscaledDt)
    {
        if (Screen.safeArea != _lastSafeArea ||
            Screen.orientation != _lastOrientation ||
            Screen.width != _lastResolution.x ||
            Screen.height != _lastResolution.y)
        {
            ApplySafeArea();
        }
    }

#if UNITY_EDITOR
    // ✅ EditMode(ExecuteAlways)에서는 기존처럼 Update로 계속 반영
    void Update()
    {
        if (Application.isPlaying) return;

        if (Screen.safeArea != _lastSafeArea ||
            Screen.orientation != _lastOrientation ||
            Screen.width != _lastResolution.x ||
            Screen.height != _lastResolution.y)
        {
            ApplySafeArea();
        }
    }
#endif

    static bool IsBad(float v) => float.IsNaN(v) || float.IsInfinity(v);
    static bool IsBad(Vector2 v) => IsBad(v.x) || IsBad(v.y);

    void ApplySafeArea()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();

        int w = Screen.width;
        int h = Screen.height;

        if (w <= 0 || h <= 0)
            return;

        Rect sa = Screen.safeArea;

        if (sa.width <= 0f || sa.height <= 0f)
            sa = new Rect(0, 0, w, h);

        Vector2 anchorMin = sa.position;
        Vector2 anchorMax = sa.position + sa.size;

        anchorMin.x /= w;
        anchorMin.y /= h;
        anchorMax.x /= w;
        anchorMax.y /= h;

        if (IsBad(anchorMin) || IsBad(anchorMax))
            return;

        anchorMin = new Vector2(Mathf.Clamp01(anchorMin.x), Mathf.Clamp01(anchorMin.y));
        anchorMax = new Vector2(Mathf.Clamp01(anchorMax.x), Mathf.Clamp01(anchorMax.y));

        if (anchorMin.x > anchorMax.x) (anchorMin.x, anchorMax.x) = (anchorMax.x, anchorMin.x);
        if (anchorMin.y > anchorMax.y) (anchorMin.y, anchorMax.y) = (anchorMax.y, anchorMin.y);

        _rect.anchorMin = anchorMin;
        _rect.anchorMax = anchorMax;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;

        _lastSafeArea = sa;
        _lastOrientation = Screen.orientation;
        _lastResolution = new Vector2Int(w, h);
    }
}
