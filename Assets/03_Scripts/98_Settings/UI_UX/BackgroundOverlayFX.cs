using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class BackgroundOverlayFX : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Graphic overlayGraphic; // Image 또는 RawImage

    [Header("Default (used by parameterless Show/Hide)")]
    [SerializeField] private float dimAlpha = 0.65f;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Header("Optional Blur (requires material)")]
    [SerializeField] private Material blurMaterialTemplate;
    [SerializeField] private string blurProperty = "_BlurAmount";
    [SerializeField] private float blurMax = 1f;
    [SerializeField] private float blurDuration = 0.15f;
    [SerializeField] private Ease blurEase = Ease.OutQuad;

    private Material _runtimeMat;
    private Tween _blurTween;

    void Reset()
    {
        group = GetComponent<CanvasGroup>();
        overlayGraphic = GetComponent<Graphic>();
    }

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (!overlayGraphic) overlayGraphic = GetComponent<Graphic>();

        // 블러 머티리얼이 있으면 런타임 인스턴스 생성(재사용 안전)
        if (blurMaterialTemplate && overlayGraphic)
        {
            _runtimeMat = new Material(blurMaterialTemplate);
            overlayGraphic.material = _runtimeMat;

            if (_runtimeMat.HasProperty(blurProperty))
                _runtimeMat.SetFloat(blurProperty, 0f);
        }

        // 초기 상태
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    // Preset에서 제어할 수 있게 Tween을 반환
    public Tween FadeIn(float targetAlpha, float duration, Ease ease)
    {
        gameObject.SetActive(true);

        group.blocksRaycasts = true;   // 뒤 입력 차단
        group.interactable = false;

        group.DOKill();
        group.alpha = 0f;

        AnimateBlur(to: blurMax, duration: duration, ease: blurEase);

        return group.DOFade(targetAlpha, duration)
                    .SetEase(ease)
                    .SetUpdate(true);
    }

    public Tween FadeOut(float duration, Ease ease)
    {
        group.interactable = false;
        group.blocksRaycasts = false;

        group.DOKill();

        AnimateBlur(to: 0f, duration: duration, ease: blurEase);

        return group.DOFade(0f, duration)
                    .SetEase(ease)
                    .SetUpdate(true)
                    .OnComplete(() => gameObject.SetActive(false));
    }

    public void Show() => FadeIn(dimAlpha, fadeDuration, fadeEase);
    public void Hide() => FadeOut(fadeDuration, fadeEase);

    public void HideImmediate()
    {
        group.DOKill();
        _blurTween?.Kill();

        if (_runtimeMat && _runtimeMat.HasProperty(blurProperty))
            _runtimeMat.SetFloat(blurProperty, 0f);

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    private void AnimateBlur(float to, float duration, Ease ease)
    {
        if (_runtimeMat == null) return;
        if (!_runtimeMat.HasProperty(blurProperty)) return;

        _blurTween?.Kill();
        float from = _runtimeMat.GetFloat(blurProperty);

        _blurTween = DOTween.To(() => from, x =>
        {
            from = x;
            _runtimeMat.SetFloat(blurProperty, x);
        }, to, duration).SetEase(ease).SetUpdate(true);
    }
}
