using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class AugmentationPopupUI : MonoBehaviour
{
    [Header("Preset")]
    [SerializeField] private AugmentationTweenPreset preset;

    [Header("Refs")]
    [SerializeField] private CanvasGroup dimmerGroup;
    [SerializeField] private BackgroundOverlayFX overlayFX;
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

    private void Reset()
    {
        panel = GetComponent<RectTransform>();
        panelGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!panel) panel = GetComponent<RectTransform>();
        if (!panelGroup) panelGroup = GetComponent<CanvasGroup>();
        if (!overlayFX && dimmerGroup) overlayFX = dimmerGroup.GetComponent<BackgroundOverlayFX>();

        _defaultPos = panel.anchoredPosition;

        for (int i = 0; i < cards.Length; i++)
            if (cards[i]) cards[i].Init(i, OnCardClicked);

        if (confirmButton)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmSelection);
        }

        ForceHidden();
    }

    public void Show()
    {
        if (_busy) return;

        _busy = true;
        gameObject.SetActive(true);

        KillSeq();
        SetInput(false);
        _selected = -1;

        if (confirmButton)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }

        foreach (var c in cards)
        {
            if (!c) continue;
            c.ResetVisual();
            c.SetInteractable(false);
        }

        if (pauseGameOnShow) GamePauseStack.Push(pauseAudioOnShow);

        var pDim = preset.Dim;
        var pPopup = preset.Popup;

        // 시작 상태
        panel.anchoredPosition = _defaultPos + pPopup.fromOffset;
        panel.localScale = Vector3.one * pPopup.fromScale;
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        _seq = DOTween.Sequence().SetUpdate(true);

        // 딤
        if (overlayFX)
            _seq.Append(overlayFX.FadeIn(pDim.targetAlpha, pDim.fadeIn, pDim.easeIn));
        else if (dimmerGroup)
        {
            dimmerGroup.alpha = 0f;
            dimmerGroup.blocksRaycasts = true;
            _seq.Append(dimmerGroup.DOFade(pDim.targetAlpha, pDim.fadeIn).SetEase(pDim.easeIn));
        }

        // 패널 (딤과 동시에 시작)
        _seq.Insert(0f, panelGroup.DOFade(1f, pPopup.fadeIn).SetEase(Ease.OutQuad));
        _seq.Insert(0f, panel.DOAnchorPos(_defaultPos, pPopup.moveIn).SetEase(pPopup.easeIn));
        _seq.Insert(0f, panel.DOScale(1f, pPopup.moveIn).SetEase(pPopup.easeIn));

        _seq.OnComplete(() =>
        {
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
            SetInput(true);
            _busy = false;
        });
    }

    public void Hide()
    {
        if (_busy) return;
        if (!preset)
        {
            gameObject.SetActive(false);
            return;
        }

        _busy = true;

        SetInput(false);
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        KillSeq();

        var pDim = preset.Dim;
        var pPopup = preset.Popup;

        _seq = DOTween.Sequence().SetUpdate(true);

        // 패널 닫기
        _seq.Join(panelGroup.DOFade(0f, pPopup.fadeOut).SetEase(pPopup.easeOut));
        _seq.Join(panel.DOAnchorPos(_defaultPos + pPopup.hideOffset, pPopup.moveOut).SetEase(pPopup.easeOut));
        _seq.Join(panel.DOScale(pPopup.hideScale, pPopup.moveOut).SetEase(pPopup.easeOut));

        // 딤 닫기
        if (overlayFX)
            _seq.Join(overlayFX.FadeOut(pDim.fadeOut, pDim.easeOut));
        else if (dimmerGroup)
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

    private void OnCardClicked(int index)
    {
        if (!_inputEnabled) return;

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

            if (_selected < 0)
            {
                cards[i].ResetVisual();
                continue;
            }

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

        if (confirmButton)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }

        foreach (var c in cards)
        {
            if (!c) continue;
            c.ResetVisual();
            c.SetInteractable(false);
        }

        gameObject.SetActive(false);
        _busy = false;
    }

    private void KillSeq()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }
    }

    [ContextMenu("TEST/Show")] private void TestShow() => Show();
    [ContextMenu("TEST/Hide")] private void TestHide() => Hide();
}
