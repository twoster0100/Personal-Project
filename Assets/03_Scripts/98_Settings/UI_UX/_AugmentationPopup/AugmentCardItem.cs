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

    private Vector3 _baseScale;

    public void Init(int index, System.Action<int> onClick)
    {
        Index = index;

        Rect = (RectTransform)transform;
        Button = GetComponent<Button>();
        Group = GetComponent<CanvasGroup>();

        _baseScale = Rect.localScale;

        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => onClick?.Invoke(Index));

        ResetVisual();
    }

    public void ResetVisual()
    {
        Rect.DOKill();
        Group.DOKill();

        Group.alpha = 1f;
        Rect.localScale = _baseScale;
    }

    public void SetInteractable(bool on)
    {
        Button.interactable = on;
        Group.interactable = on;
        Group.blocksRaycasts = on;
    }

    public void DimOther(bool dim, float duration, float dimAlpha)
    {
        Group.DOKill();

        float target = dim ? dimAlpha : 1f;
        if (duration <= 0f)
        {
            Group.alpha = target;
            return;
        }

        Group.DOFade(target, duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void Emphasize(bool on, float duration, float scale)
    {
        Rect.DOKill();

        Vector3 target = on ? _baseScale * scale : _baseScale;
        if (duration <= 0f)
        {
            Rect.localScale = target;
            return;
        }

        Rect.DOScale(target, duration)
            .SetEase(on ? Ease.OutBack : Ease.OutQuad)
            .SetUpdate(true);
    }
}
