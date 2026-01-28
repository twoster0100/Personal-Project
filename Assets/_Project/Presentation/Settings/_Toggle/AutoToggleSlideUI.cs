using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class AutoToggleSlideUI : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private AutoModeController autoMode;

    [Header("Click Area")]
    [SerializeField] private Button toggleButton;

    [Header("Visuals")]
    [SerializeField] private RectTransform selection;   // 움직일 노브
    [SerializeField] private RectTransform onTarget;    // 노브가 도착할 위치(ON)
    [SerializeField] private RectTransform offTarget;   // 노브가 도착할 위치(OFF)
    [SerializeField] private GameObject onVisual;       // ON 글자 이미지
    [SerializeField] private GameObject offVisual;      // OFF 글자 이미지

    [Header("Tween")]
    [SerializeField] private float slideDuration = 0.12f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private bool useUnscaledTime = true;

    private Tweener _tween;

    private void Awake()
    {
        if (!toggleButton) toggleButton = GetComponent<Button>();
        toggleButton.onClick.AddListener(() => autoMode.ToggleAuto());
    }

    private void OnEnable()
    {
        autoMode.onAutoChanged.AddListener(Apply);
        ApplyImmediate(autoMode.IsAuto);
    }

    private void OnDisable()
    {
        autoMode.onAutoChanged.RemoveListener(Apply);
        _tween?.Kill();
    }

    private void Apply(bool isAuto)
    {
        // ON/OFF 글자 이미지 표시 방식
        if (onVisual) onVisual.SetActive(isAuto);
        if (offVisual) offVisual.SetActive(!isAuto);

        // 노브 슬라이드
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
