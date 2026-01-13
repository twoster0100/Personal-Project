using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class AugmentCardItem : MonoBehaviour
{
    public int Index { get; private set; }
    public Button Button { get; private set; }
    public CanvasGroup Group { get; private set; }
    public RectTransform Rect { get; private set; }

    private Vector3 _defaultScale;

    public void Init(int index, System.Action<int> onClick)
    {
        Index = index;
        Rect = (RectTransform)transform;
        Button = GetComponent<Button>();
        Group = GetComponent<CanvasGroup>();

        _defaultScale = Rect.localScale;

        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => onClick?.Invoke(Index));
    }

    public void SetInteractable(bool on)
    {
        Button.interactable = on;
        if (Group)
        {
            Group.interactable = on;
            Group.blocksRaycasts = on;
        }
    }

    public void SetHiddenInstant()
    {
        if (Group) Group.alpha = 0f;
        Rect.localScale = _defaultScale * 0.92f;
    }

    public Tween PlayPopIn(float duration)
    {
        Rect.DOKill();
        Group?.DOKill();

        Rect.localScale = _defaultScale * 0.92f;
        if (Group) Group.alpha = 0f;

        var s = Rect.DOScale(_defaultScale, duration).SetEase(Ease.OutBack);
        if (Group)
        {
            var f = Group.DOFade(1f, duration * 0.8f).SetEase(Ease.OutQuad);
            return DOTween.Sequence().Join(s).Join(f);
        }
        return s;
    }

    public void DimOther(bool dim, float duration, float dimAlpha)
    {
        if (!Group) return;
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
