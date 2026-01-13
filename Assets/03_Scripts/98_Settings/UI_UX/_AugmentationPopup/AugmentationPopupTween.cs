using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class AugmentationPopupTween : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private RectTransform panel;   // 보통 Augmentation_Root 자신
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Show Motion")]
    [SerializeField] private float showDuration = 0.35f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private float showFadeDuration = 0.20f;
    [SerializeField] private Vector2 showFromOffset = new Vector2(0f, -80f);
    [SerializeField] private float showFromScale = 0.96f;

    [Header("Hide Motion")]
    [SerializeField] private float hideDuration = 0.20f;
    [SerializeField] private Ease hideEase = Ease.InQuad;
    [SerializeField] private float hideFadeDuration = 0.15f;
    [SerializeField] private Vector2 hideToOffset = new Vector2(0f, -60f);
    [SerializeField] private float hideToScale = 0.98f;

    [Header("Behavior")]
    [SerializeField] private bool deactivateOnHidden = true;
    [SerializeField] private bool startHidden = true;

    private Vector2 _anchoredPosDefault;
    private Sequence _seq;

    void Reset()
    {
        panel = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (!panel) panel = GetComponent<RectTransform>();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

        _anchoredPosDefault = panel.anchoredPosition;

        if (startHidden)
        {
            ForceHidden();
        }
    }
    public void Show()
    {
        if (deactivateOnHidden && !gameObject.activeSelf)
            gameObject.SetActive(true);

        KillSeq();
        // 시작 상태 세팅
        panel.anchoredPosition = _anchoredPosDefault + showFromOffset;
        panel.localScale = Vector3.one * showFromScale;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        _seq = DOTween.Sequence().SetUpdate(true); // SetUpdate(true)=Time.timeScale 영향 없이 UI 연출
        _seq.Join(canvasGroup.DOFade(1f, showFadeDuration).SetEase(Ease.OutQuad));
        _seq.Join(panel.DOAnchorPos(_anchoredPosDefault, showDuration).SetEase(showEase));
        _seq.Join(panel.DOScale(1f, showDuration).SetEase(showEase));
        _seq.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        });
    }

    public void Hide()
    {
        KillSeq();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        _seq = DOTween.Sequence().SetUpdate(true);
        _seq.Join(canvasGroup.DOFade(0f, hideFadeDuration).SetEase(Ease.InQuad));
        _seq.Join(panel.DOAnchorPos(_anchoredPosDefault + hideToOffset, hideDuration).SetEase(hideEase));
        _seq.Join(panel.DOScale(hideToScale, hideDuration).SetEase(hideEase));
        _seq.OnComplete(() =>
        {
            if (deactivateOnHidden)
                gameObject.SetActive(false);
        });
    }

    public void ForceHidden()
    {
        KillSeq();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        panel.anchoredPosition = _anchoredPosDefault;
        panel.localScale = Vector3.one;

        if (deactivateOnHidden)
            gameObject.SetActive(false);
    }
    private void KillSeq()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }
    }

    // 인스펙터 우클릭 메뉴로 테스트
    [ContextMenu("TEST/Show")]
    private void TestShow() => Show();

    [ContextMenu("TEST/Hide")]
    private void TestHide() => Hide();
}
