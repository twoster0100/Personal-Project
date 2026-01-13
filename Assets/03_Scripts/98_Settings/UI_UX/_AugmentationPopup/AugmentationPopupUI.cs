using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class AugmentationPopupUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BackgroundOverlayFX overlay;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private AugmentCardItem[] cards;
    [SerializeField] private Button confirmButton; // 선택사항(없으면 같은 카드 2번 클릭으로 확정)

    [Header("Show/Hide")]
    [SerializeField] private float showDuration = 0.35f;
    [SerializeField] private float hideDuration = 0.20f;
    [SerializeField] private Vector2 showFromOffset = new Vector2(0, -80);
    [SerializeField] private float showFromScale = 0.96f;

    [Header("Cards")]
    [SerializeField] private float cardPopDuration = 0.22f;
    [SerializeField] private float cardStagger = 0.08f;

    [Header("Selection Visual")]
    [SerializeField] private float dimOthersAlpha = 0.35f;
    [SerializeField] private float emphasizeScale = 1.06f;

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

        _defaultPos = panel.anchoredPosition;

        for (int i = 0; i < cards.Length; i++)
            cards[i].Init(i, OnCardClicked);

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

        gameObject.SetActive(true);

        overlay?.Show();
        if (pauseGameOnShow) GamePauseStack.Push(pauseAudioOnShow);

        KillSeq();
        SetInput(false);
        _selected = -1;

        if (confirmButton)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }

        // 초기화
        panel.anchoredPosition = _defaultPos + showFromOffset;
        panel.localScale = Vector3.one * showFromScale;
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        foreach (var c in cards) c.SetHiddenInstant();

        // 시퀀스
        _seq = DOTween.Sequence().SetUpdate(true);

        _seq.Join(panelGroup.DOFade(1f, 0.20f).SetEase(Ease.OutQuad));
        _seq.Join(panel.DOAnchorPos(_defaultPos, showDuration).SetEase(Ease.OutBack));
        _seq.Join(panel.DOScale(1f, showDuration).SetEase(Ease.OutBack));

        // 카드 스태거 팝인
        for (int i = 0; i < cards.Length; i++)
        {
            _seq.Insert(0.18f + i * cardStagger, cards[i].PlayPopIn(cardPopDuration));
        }

        float endTime = 0.18f + (cards.Length - 1) * cardStagger + cardPopDuration;
        _seq.AppendInterval(Mathf.Max(0f, endTime - _seq.Duration()));

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
        _busy = true;

        SetInput(false);
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        KillSeq();

        _seq = DOTween.Sequence().SetUpdate(true);
        _seq.Join(panelGroup.DOFade(0f, 0.12f).SetEase(Ease.InQuad));
        _seq.Join(panel.DOAnchorPos(_defaultPos + new Vector2(0, -60f), hideDuration).SetEase(Ease.InQuad));
        _seq.Join(panel.DOScale(0.98f, hideDuration).SetEase(Ease.InQuad));

        _seq.OnComplete(() =>
        {
            overlay?.Hide();
            if (pauseGameOnShow) GamePauseStack.Pop();

            gameObject.SetActive(false);
            _busy = false;
        });
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
        for (int i = 0; i < cards.Length; i++)
        {
            bool sel = (i == _selected);
            cards[i].Emphasize(sel, 0.12f, emphasizeScale);
            cards[i].DimOther(!sel, 0.12f, dimOthersAlpha);
        }
    }

    private void ConfirmSelection()
    {
        if (_selected < 0) return;
        if (!_inputEnabled) return;

        SetInput(false);

        // 짧은 확정 “툭” 후 닫기
        cards[_selected].Emphasize(true, 0.10f, 1.10f);

        DOTween.Sequence().SetUpdate(true)
            .AppendInterval(0.10f)
            .OnComplete(() =>
            {
                // TODO: 여기서 실제 증강 적용(데이터/이벤트) 호출
                Hide();
            });
    }

    private void SetInput(bool on)
    {
        _inputEnabled = on;
        foreach (var c in cards) c.SetInteractable(on);
    }

    private void ForceHidden()
    {
        KillSeq();

        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        panel.anchoredPosition = _defaultPos;
        panel.localScale = Vector3.one;

        foreach (var c in cards)
        {
            c.SetHiddenInstant();
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
