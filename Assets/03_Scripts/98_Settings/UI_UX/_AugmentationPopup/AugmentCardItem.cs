using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class AugmentCardItem : MonoBehaviour
{
    public int Index { get; private set; }
    public Button Button { get; private set; }
    public CanvasGroup Group { get; private set; }
    public RectTransform Rect { get; private set; }

    private Vector3 _defaultScale;
    private Vector2 _defaultPos;

    public void Init(int index, System.Action<int> onClick)
    {
        Index = index;

        Rect = (RectTransform)transform;
        Button = GetComponent<Button>();
        Group = GetComponent<CanvasGroup>();

        CacheDefaultsFromCurrent();

        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => onClick?.Invoke(Index));
    }

    public void CacheDefaultsFromCurrent()
    {
        if (!Rect) Rect = (RectTransform)transform;
        _defaultScale = Rect.localScale;
        _defaultPos = Rect.anchoredPosition;
    }

    public void SetInteractable(bool on)
    {
        Button.interactable = on;
        Group.interactable = on;
        Group.blocksRaycasts = on;
    }

    public void SetHiddenInstant(float fromScale, float fromYOffset)
    {
        Group.alpha = 0f;
        Rect.localScale = _defaultScale * fromScale;
        Rect.anchoredPosition = _defaultPos + new Vector2(0f, fromYOffset);
    }

    public Tween PlayPopIn(float duration, float fromScale, float fromYOffset, Ease ease)
    {
        Rect.DOKill();
        Group.DOKill();

        // 시작 상태 강제
        Group.alpha = 0f;
        Rect.localScale = _defaultScale * fromScale;
        Rect.anchoredPosition = _defaultPos + new Vector2(0f, fromYOffset);

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(Group.DOFade(1f, duration * 0.7f).SetEase(Ease.OutQuad));
        seq.Join(Rect.DOScale(_defaultScale, duration).SetEase(ease));
        seq.Join(Rect.DOAnchorPos(_defaultPos, duration * 0.9f).SetEase(Ease.OutCubic));
        return seq;
    }

    public void DimOther(bool dim, float duration, float dimAlpha)
    {
        Group.DOKill();
        Group.DOFade(dim ? dimAlpha : 1f, duration).SetEase(Ease.OutQuad);
    }

    public void Emphasize(bool on, float duration, float scale)
    {
        Rect.DOKill();
        Rect.DOScale(on ? _defaultScale * scale : _defaultScale, duration)
            .SetEase(on ? Ease.OutBack : Ease.OutQuad);
    }
}
