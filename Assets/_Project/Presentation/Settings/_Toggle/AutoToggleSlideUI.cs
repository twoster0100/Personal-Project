using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AutoToggleSlideUI : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private AutoModeController autoMode;

    [Header("Click Area")]
    [SerializeField] private Button toggleButton; // 이제 View에서는 참조만(입력 처리는 다른 컴포넌트)

    [Header("Visuals")]
    [SerializeField] private RectTransform selection;
    [SerializeField] private RectTransform onTarget;
    [SerializeField] private RectTransform offTarget;
    [SerializeField] private GameObject onVisual;
    [SerializeField] private GameObject offVisual;

    [Header("Tween")]
    [SerializeField] private float slideDuration = 0.12f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private bool useUnscaledTime = true;

    private Tweener _tween;

    private void Awake()
    {
        if (!toggleButton) toggleButton = GetComponent<Button>();
        // ❗여기서 onClick으로 모델을 바꾸지 않는다.
    }

    private void OnEnable()
    {
        if (autoMode == null)
        {
            Debug.LogError("[AutoToggleSlideUI] AutoModeController is not assigned.");
            return;
        }

        autoMode.onAutoChanged.AddListener(Apply);
        ApplyImmediate(autoMode.IsAuto);
    }

    private void OnDisable()
    {
        if (autoMode != null)
            autoMode.onAutoChanged.RemoveListener(Apply);

        _tween?.Kill();
    }

    private void Apply(bool isAuto)
    {
        if (onVisual) onVisual.SetActive(isAuto);
        if (offVisual) offVisual.SetActive(!isAuto);

        var target = (isAuto ? onTarget : offTarget).anchoredPosition;

        _tween?.Kill();
        _tween = DOTween.To(() => selection.anchoredPosition, v => selection.anchoredPosition = v, target, slideDuration)
            .SetEase(ease);

        if (useUnscaledTime)
            _tween.SetUpdate(true);
    }

    private void ApplyImmediate(bool isAuto)
    {
        if (onVisual) onVisual.SetActive(isAuto);
        if (offVisual) offVisual.SetActive(!isAuto);

        selection.anchoredPosition = (isAuto ? onTarget : offTarget).anchoredPosition;
    }
}
