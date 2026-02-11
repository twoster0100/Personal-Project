using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.Presentation.Combat
{
    /// <summary>
    /// 오프라인 정산 UI 표시 전용 Presenter.
    /// - 어댑터 이벤트를 받아 패널 텍스트를 채우고 표시한다.
    /// </summary>
    public sealed class OfflineSettlementPopupPresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private OfflineSettlementUiEventAdapter adapter;

        [Header("UI Root")]
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


        public void Configure(
            OfflineSettlementUiEventAdapter adapterRef,
            GameObject panelRootRef,
            Button closeButtonRef,
            TMP_Text elapsedTextRef,
            TMP_Text goldTextRef,
            TMP_Text expTextRef,
            TMP_Text dropTextRef)
        {
            adapter = adapterRef;
            panelRoot = panelRootRef;
            closeButton = closeButtonRef;
            elapsedText = elapsedTextRef;
            goldText = goldTextRef;
            expText = expTextRef;
            dropText = dropTextRef;
        }

        private void Reset()
        {
            if (adapter == null)
                adapter = FindObjectOfType<OfflineSettlementUiEventAdapter>(true);
        }

        private void OnEnable()
        {
            if (adapter != null)
                adapter.SettlementRaised += OnSettlementRaised;

            if (closeButton != null)
                closeButton.onClick.AddListener(HidePanel);

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

            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        public void HidePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}
