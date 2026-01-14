using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class AugmentationPopupUI : MonoBehaviour
{
    [SerializeField] private HorizontalLayoutGroup cardsLayout;

    [Header("Preset")]
    [SerializeField] private AugmentationTweenPreset preset;

    [Header("Refs")]
    [SerializeField] private CanvasGroup dimmerGroup; // UI_OverlayFX(CanvasGroup)
    [SerializeField] private BackgroundOverlayFX overlayFX; // UI_OverlayFX에 붙어있으면 자동 연결
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private AugmentCardItem[] cards;
    [SerializeField] private Button confirmButton;

    [Header("Pause")]
    [SerializeField] private bool pauseGameOnShow = true;
    [SerializeField] private bool pauseAudioOnShow = false;

    private Vector2 _defaultPos;
    private Sequence _seq;
    private bool _busy;
    private bool _inputEnabled;
    private int _selected = -1;

    void Reset()
    {
        panel = GetComponent<RectTransform>();
        panelGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (!panel) panel = GetComponent<RectTransform>();
        if (!panelGroup) panelGroup = GetComponent<CanvasGroup>();

        // dimmerGroup이 연결되어 있고, 같은 오브젝트에 BackgroundOverlayFX가 있으면 자동으로 잡아줌
        if (!overlayFX && dimmerGroup) overlayFX = dimmerGroup.GetComponent<BackgroundOverlayFX>();

        _defaultPos = panel.anchoredPosition;

        for (int i = 0; i < cards.Length; i++)
            if (cards[i]) cards[i].Init(i, OnCardClicked);

        if (confirmButton)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmSelection);
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }

        ForceHidden();
    }

    public void Show()
    {
        if (_busy) return;
        _busy = true;

        if (!preset)
        {
            Debug.LogWarning("[AugmentationPopupUI] Preset이 비어있음");
            _busy = false;
            return;
        }

        gameObject.SetActive(true);

        if (pauseGameOnShow) GamePauseStack.Push(pauseAudioOnShow);

        KillSeq();
        SetInput(false);
        _selected = -1;

        if (confirmButton)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }

        // 레이아웃이 먼저 “정상 위치”를 잡게 만들고 → 그 위치를 기본값으로 캐시 → 애니 중엔 레이아웃 OFF
        PrepareCardsForAnimation();

        var pDim = preset.Dim;
        var pPopup = preset.Popup;
        var pCards = preset.Cards;

        // 팝업 패널 시작 상태
        panel.anchoredPosition = _defaultPos + pPopup.fromOffset;
        panel.localScale = Vector3.one * pPopup.fromScale;
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        // 카드 시작 상태 (절대 SetActive(false)로 껐다 켜지 않음)
        foreach (var c in cards)
        {
            if (!c) continue;
            if (!c.gameObject.activeSelf) c.gameObject.SetActive(true);
            c.SetHiddenInstant(pCards.fromScale, pCards.fromYOffset);
            c.SetInteractable(false);
        }

        _seq = DOTween.Sequence().SetUpdate(true);

        // 딤: BackgroundOverlayFX가 있으면 그걸로(비활성 문제 자동 해결)
        if (overlayFX)
            _seq.Append(overlayFX.FadeIn(pDim.targetAlpha, pDim.fadeIn, pDim.easeIn));
        else
        {
            dimmerGroup.alpha = 0f;
            dimmerGroup.blocksRaycasts = true;
            _seq.Append(dimmerGroup.DOFade(pDim.targetAlpha, pDim.fadeIn).SetEase(pDim.easeIn));
        }

        // 팝업 등장(살짝 겹쳐 시작)
        _seq.Insert(0.05f, panelGroup.DOFade(1f, pPopup.fadeIn).SetEase(Ease.OutQuad));
        _seq.Insert(0.05f, panel.DOAnchorPos(_defaultPos, pPopup.moveIn).SetEase(pPopup.easeIn));
        _seq.Insert(0.05f, panel.DOScale(1f, pPopup.moveIn).SetEase(pPopup.easeIn));

        // 카드 “툭툭” (스태거)
        for (int i = 0; i < cards.Length; i++)
        {
            if (!cards[i]) continue;
            float t = pCards.startDelay + i * pCards.stagger;
            _seq.Insert(t, cards[i].PlayPopIn(pCards.popDuration, pCards.fromScale, pCards.fromYOffset, pCards.ease));
        }

        float endTime = pCards.startDelay + Mathf.Max(0, cards.Length - 1) * pCards.stagger + pCards.popDuration;
        _seq.AppendInterval(Mathf.Max(0f, endTime - _seq.Duration()));

        _seq.OnComplete(() =>
        {
            FinishCardsAfterAnimation();

            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
            SetInput(true);
            _busy = false;
        });
    }

    public void Hide()
    {
        if (_busy) return;
        _busy = true;

        SetInput(false);
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        KillSeq();

        var pDim = preset.Dim;
        var pPopup = preset.Popup;

        _seq = DOTween.Sequence().SetUpdate(true);

        // 팝업 닫기
        _seq.Join(panelGroup.DOFade(0f, pPopup.fadeOut).SetEase(pPopup.easeOut));
        _seq.Join(panel.DOAnchorPos(_defaultPos + pPopup.hideOffset, pPopup.moveOut).SetEase(pPopup.easeOut));
        _seq.Join(panel.DOScale(pPopup.hideScale, pPopup.moveOut).SetEase(pPopup.easeOut));

        // 딤 닫기
        if (overlayFX)
            _seq.Join(overlayFX.FadeOut(pDim.fadeOut, pDim.easeOut));
        else
            _seq.Join(dimmerGroup.DOFade(0f, pDim.fadeOut).SetEase(pDim.easeOut));

        _seq.OnComplete(() =>
        {
            if (pauseGameOnShow) GamePauseStack.Pop();

            if (!overlayFX && dimmerGroup)
                dimmerGroup.blocksRaycasts = false;

            gameObject.SetActive(false);
            _busy = false;
        });
    }

    // 레이아웃(자동 배치)이 카드 위치를 덮어쓰지 않게: 애니 중 OFF
    private void PrepareCardsForAnimation()
    {
        if (!cardsLayout) return;

        // 1) 레이아웃 ON → 위치 계산
        cardsLayout.enabled = true;

        foreach (var c in cards)
            if (c && !c.gameObject.activeSelf) c.gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)cardsLayout.transform);

        // 2) “현재 위치”를 기본값으로 캐시
        foreach (var c in cards)
            if (c) c.CacheDefaultsFromCurrent();

        // 3) 애니 중엔 레이아웃 OFF (anchoredPosition 애니가 유지됨)
        cardsLayout.enabled = false;
    }

    // 애니 끝나면 다시 레이아웃 ON
    private void FinishCardsAfterAnimation()
    {
        if (!cardsLayout) return;
        cardsLayout.enabled = true;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)cardsLayout.transform);
    }

    private void OnCardClicked(int index)
    {
        if (!_inputEnabled) return;

        // 선택 변경
        if (_selected != index)
        {
            _selected = index;
            ApplySelectionVisual();

            if (confirmButton)
            {
                confirmButton.gameObject.SetActive(true);
                confirmButton.interactable = true;
            }
            return;
        }

        // confirm 버튼이 없으면: 같은 카드 2번 클릭 = 확정
        if (!confirmButton)
            ConfirmSelection();
    }

    private void ApplySelectionVisual()
    {
        var s = preset.Selection;

        for (int i = 0; i < cards.Length; i++)
        {
            if (!cards[i]) continue;
            bool sel = (i == _selected);
            cards[i].Emphasize(sel, s.tweenDuration, s.emphasizeScale);
            cards[i].DimOther(!sel, s.tweenDuration, s.dimOthersAlpha);
        }
    }

    private void ConfirmSelection()
    {
        if (_selected < 0) return;
        if (!_inputEnabled) return;

        SetInput(false);

        var c = preset.Confirm;

        cards[_selected].Emphasize(true, c.emphasizeDuration, c.emphasizeScale);

        DOTween.Sequence().SetUpdate(true)
            .AppendInterval(c.delay)
            .OnComplete(() =>
            {
                // TODO: 여기서 실제 증강 적용(데이터/이벤트) 호출
                Hide();
            });
    }

    private void SetInput(bool on)
    {
        _inputEnabled = on;
        foreach (var c in cards)
            if (c) c.SetInteractable(on);
    }

    private void ForceHidden()
    {
        KillSeq();

        if (overlayFX) overlayFX.HideImmediate();
        else if (dimmerGroup)
        {
            dimmerGroup.alpha = 0f;
            dimmerGroup.blocksRaycasts = false;
        }

        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        panel.anchoredPosition = _defaultPos;
        panel.localScale = Vector3.one;

        foreach (var c in cards)
        {
            if (!c) continue;
            c.SetHiddenInstant(preset ? preset.Cards.fromScale : 1f, preset ? preset.Cards.fromYOffset : 0f);
            c.SetInteractable(false);
        }

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

    [ContextMenu("TEST/Show")]
    private void TestShow() => Show();

    [ContextMenu("TEST/Hide")]
    private void TestHide() => Hide();
}
