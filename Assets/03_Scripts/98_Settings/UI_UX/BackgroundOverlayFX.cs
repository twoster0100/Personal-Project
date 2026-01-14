using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class BackgroundOverlayFX : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Graphic overlayGraphic;

    [Header("Optional Blur (requires material)")]
    [SerializeField] private Material blurMaterialTemplate;
    [SerializeField] private string blurProperty = "_BlurAmount";
    [SerializeField] private float blurMax = 1f;
    [SerializeField] private float blurDuration = 0.15f;

    private Material _runtimeMat;
    private Tween _blurTween;

    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
        overlayGraphic = GetComponent<Graphic>();
    }

    private void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (!overlayGraphic) overlayGraphic = GetComponent<Graphic>();

        if (blurMaterialTemplate && overlayGraphic)
        {
            _runtimeMat = new Material(blurMaterialTemplate);
            overlayGraphic.material = _runtimeMat;

            if (_runtimeMat.HasProperty(blurProperty))
                _runtimeMat.SetFloat(blurProperty, 0f);
        }

        HideImmediate();
    }

    public Tween FadeIn(float targetAlpha, float duration, Ease ease)
    {
        gameObject.SetActive(true);

        group.interactable = false;
        group.blocksRaycasts = true;

        group.DOKill();
        var t = group.DOFade(targetAlpha, duration).SetEase(ease).SetUpdate(true);

        AnimateBlur(to: blurMax);
        return t;
    }

    public Tween FadeOut(float duration, Ease ease)
    {
        group.interactable = false;
        group.blocksRaycasts = true; // 페이드 끝날 때까지는 입력 막는 게 안전

        group.DOKill();
        var t = group.DOFade(0f, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() =>
            {
                group.blocksRaycasts = false;
                gameObject.SetActive(false);
            });

        AnimateBlur(to: 0f);
        return t;
    }

    public void HideImmediate()
    {
        group.DOKill();
        _blurTween?.Kill();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (_runtimeMat && _runtimeMat.HasProperty(blurProperty))
            _runtimeMat.SetFloat(blurProperty, 0f);

        gameObject.SetActive(false);
    }

    private void AnimateBlur(float to)
    {
        if (_runtimeMat == null) return;
        if (!_runtimeMat.HasProperty(blurProperty)) return;

        _blurTween?.Kill();

        float from = _runtimeMat.GetFloat(blurProperty);
        _blurTween = DOTween.To(() => from, x =>
        {
            from = x;
            _runtimeMat.SetFloat(blurProperty, x);
        }, to, blurDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }
}
