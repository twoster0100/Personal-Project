using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(menuName = "UI/Augmentation Tween Preset", fileName = "AugmentationTweenPreset")]
public class AugmentationTweenPreset : ScriptableObject
{
    [SerializeField] private DimSettings dim = new DimSettings();
    [SerializeField] private PopupSettings popup = new PopupSettings();
    [SerializeField] private CardsSettings cards = new CardsSettings();
    [SerializeField] private SelectionSettings selection = new SelectionSettings();
    [SerializeField] private ConfirmSettings confirm = new ConfirmSettings();

    public DimSettings Dim => dim;
    public PopupSettings Popup => popup;
    public CardsSettings Cards => cards;
    public SelectionSettings Selection => selection;
    public ConfirmSettings Confirm => confirm;

    [System.Serializable]
    public class DimSettings
    {
        public float targetAlpha = 0.65f;
        public float fadeIn = 0.15f;
        public float fadeOut = 0.15f;
        public Ease easeIn = Ease.OutQuad;
        public Ease easeOut = Ease.OutQuad;
    }

    [System.Serializable]
    public class PopupSettings
    {
        public float fadeIn = 0.20f;
        public float fadeOut = 0.12f;

        public float moveIn = 0.35f;
        public float moveOut = 0.20f;

        public Vector2 fromOffset = new Vector2(0, -80);
        public Vector2 hideOffset = new Vector2(0, -60);

        public float fromScale = 0.96f;
        public float hideScale = 0.98f;

        public Ease easeIn = Ease.OutBack;
        public Ease easeOut = Ease.InQuad;
    }

    [System.Serializable]
    public class CardsSettings
    {
        // 툭툭 연출 (안 보이면 여기 3개를 먼저 키움)
        public float startDelay = 0.28f;
        public float popDuration = 0.32f;
        public float stagger = 0.12f;

        public float fromScale = 0.70f;
        public float fromYOffset = -80f;

        public Ease ease = Ease.OutBack;
    }

    [System.Serializable]
    public class SelectionSettings
    {
        public float tweenDuration = 0.12f;
        public float dimOthersAlpha = 0.35f;
        public float emphasizeScale = 1.06f;
    }

    [System.Serializable]
    public class ConfirmSettings
    {
        public float emphasizeDuration = 0.10f;
        public float emphasizeScale = 1.10f;
        public float delay = 0.10f;
    }
}
