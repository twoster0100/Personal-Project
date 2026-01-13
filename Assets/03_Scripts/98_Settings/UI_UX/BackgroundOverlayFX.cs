using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class BackgroundOverlayFX : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Graphic overlayGraphic; // Image 또는 RawImage

    [Header("Dim")]
    [SerializeField] private float dimAlpha = 0.65f;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Header("Optional Blur (requires material)")]
    [SerializeField] private Material blurMaterialTemplate; // 인스펙터에 블러 머티리얼 넣기(선택)
    [SerializeField] private string blurProperty = "_BlurAmount"; // 셰이더 프로퍼티 이름
    [SerializeField] private float blurMax = 1f;
    [SerializeField] private float blurDuration = 0.15f;

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

    public void Show()
    {
        gameObject.SetActive(true);

        group.blocksRaycasts = true;   // 뒤 입력 차단
        group.interactable = false;

        group.DOKill();
        group.DOFade(dimAlpha, fadeDuration)
             .SetEase(fadeEase)
             .SetUpdate(true); // timescale 0에서도 동작

        AnimateBlur(to: blurMax);
    }

    public void Hide()
    {
        group.interactable = false;
        group.blocksRaycasts = false;

        group.DOKill();
        group.DOFade(0f, fadeDuration)
             .SetEase(fadeEase)
             .SetUpdate(true)
             .OnComplete(() => gameObject.SetActive(false));

        AnimateBlur(to: 0f);
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
