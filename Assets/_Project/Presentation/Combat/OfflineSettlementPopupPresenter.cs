using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.Presentation.Combat
{
    /// <summary>
    /// 오프라인 정산 UI 표시 전용 Presenter.
    /// - 어댑터 이벤트를 받아 패널 텍스트를 채우고 표시한다.
    /// - 배경 Dimmer(뒤 화면 어둡게)를 함께 제어한다.
    /// </summary>
    public sealed class OfflineSettlementPopupPresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private OfflineSettlementUiEventAdapter adapter;

        [Header("UI Root")]
        [SerializeField] private GameObject dimmerRoot;
        [SerializeField] private Button dimmerCloseButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text elapsedText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private TMP_Text dropText;

        [Header("Format")]
        [SerializeField] private string elapsedFormat = "{0}h {1}m {2}s";
        [SerializeField] private string goldFormat = "+{0:N0}";
        [SerializeField] private string expFormat = "+{0:N0}";
        [SerializeField] private string dropFormat = "+{0:N0}";
        [SerializeField] private bool hidePanelOnStart = true;
        [SerializeField] private bool replayLatestOnEnable = true;
        [SerializeField] private bool allowCloseByDimmer = false;

        private CanvasGroup _panelCanvasGroup;
        private void Reset()
        {
            AutoBindIfNeeded();
        }
        private void Awake()
        {
            AutoBindIfNeeded();
            EnsurePanelCanvasGroup();
        }

        private void OnEnable()
        {
            AutoBindIfNeeded();

            if (adapter != null)
                adapter.SettlementRaised += OnSettlementRaised;
            else
                Debug.LogWarning("[OfflineSettlementPopup] Adapter is null. Popup will not receive settlement event.");

            if (closeButton != null)
                closeButton.onClick.AddListener(HidePanel);

            if (allowCloseByDimmer && dimmerCloseButton != null)
                dimmerCloseButton.onClick.AddListener(HidePanel);

            if (hidePanelOnStart)
                HidePanel();

            if (replayLatestOnEnable && adapter != null)
                adapter.TryReplayLatest(OnSettlementRaised);
        }

        private void OnDisable()
        {
            if (adapter != null)
                adapter.SettlementRaised -= OnSettlementRaised;

            if (closeButton != null)
                closeButton.onClick.RemoveListener(HidePanel);

            if (allowCloseByDimmer && dimmerCloseButton != null)
                dimmerCloseButton.onClick.RemoveListener(HidePanel);
        }
        private void AutoBindIfNeeded()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (adapter == null)
                adapter = FindObjectOfType<OfflineSettlementUiEventAdapter>(true);

            if (dimmerRoot == null && transform.parent != null)
            {
                Transform t = transform.parent.Find("OfflineReward_Dimmer");
                if (t != null) dimmerRoot = t.gameObject;
            }

            if (closeButton == null)

                closeButton = GetComponentInChildren<Button>(true);
        }

        private void EnsurePanelCanvasGroup()
        {
            if (panelRoot == null)
                return;

            _panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (_panelCanvasGroup == null)
                _panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
        }
        private void OnSettlementRaised(OfflineSettlementUiPayload payload)
        {
            if (!payload.HasReward)
                return;

            long seconds = payload.cappedSeconds > 0 ? payload.cappedSeconds : payload.elapsedSeconds;
            long hour = seconds / 3600;
            long minute = (seconds % 3600) / 60;
            long second = seconds % 60;

            if (elapsedText != null)
                elapsedText.text = string.Format(elapsedFormat, hour, minute, second);

            if (goldText != null)
                goldText.text = string.Format(goldFormat, payload.gold);

            if (expText != null)
                expText.text = string.Format(expFormat, payload.exp);

            if (dropText != null)
                dropText.text = string.Format(dropFormat, payload.drop);

            SetVisible(true);
        }

        public void HidePanel()
        {
            SetVisible(false);
        }
        private void SetVisible(bool visible)
        {
            if (dimmerRoot != null)
                dimmerRoot.SetActive(visible);

            if (_panelCanvasGroup == null)
                EnsurePanelCanvasGroup();

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = visible ? 1f : 0f;
                _panelCanvasGroup.interactable = visible;
                _panelCanvasGroup.blocksRaycasts = visible;
            }
            else if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(visible);
            }
        }
    }
}
