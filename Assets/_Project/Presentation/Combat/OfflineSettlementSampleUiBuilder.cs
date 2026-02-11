using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.Presentation.Combat
{
    /// <summary>
    /// Combat_Test 씬에서 오프라인 보상 샘플 UI를 빠르게 생성하는 유틸.
    /// - ContextMenu로 1회 생성하고, 생성된 오브젝트를 프리팹/씬에 고정해서 사용한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OfflineSettlementSampleUiBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private CombatBootPresenter combatBootPresenter;

        [Header("Build")]
        [SerializeField] private string panelObjectName = "OfflineSettlementPanel";

        [ContextMenu("Build Sample Offline Settlement UI")]
        public void BuildSampleUi()
        {
            EnsureReferences();
            if (targetCanvas == null)
            {
                Debug.LogError("[OfflineSettlementUI] Canvas not found. Assign targetCanvas first.");
                return;
            }

            Transform root = targetCanvas.transform;
            Transform exists = root.Find(panelObjectName);
            if (exists != null)
            {
                Debug.LogWarning($"[OfflineSettlementUI] '{panelObjectName}' already exists. Skip create.");
                return;
            }

            var panelGo = CreateUiRoot(root, panelObjectName, new Vector2(540f, 420f));
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color32(40, 33, 28, 230);

            var title = CreateText(panelGo.transform, "Title", "휴식 보상", 34, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(480f, 56f));

            var elapsedLabel = CreateText(panelGo.transform, "ElapsedLabel", "휴식 시간", 24, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(elapsedLabel.rectTransform, new Vector2(0.1f, 0.72f), new Vector2(0.1f, 0.72f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(240f, 40f));

            var elapsedValue = CreateText(panelGo.transform, "ElapsedValue", "0h 0m 0s", 28, FontStyles.Normal, TextAlignmentOptions.Right);
            SetRect(elapsedValue.rectTransform, new Vector2(0.9f, 0.72f), new Vector2(0.9f, 0.72f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(260f, 40f));

            var rewardHeader = CreateText(panelGo.transform, "RewardHeader", "획득 보상", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(rewardHeader.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 40f));

            var goldValue = CreateRewardRow(panelGo.transform, "GoldRow", "Gold", "+0", new Vector2(0.5f, 0.46f));
            var expValue = CreateRewardRow(panelGo.transform, "ExpRow", "Exp", "+0", new Vector2(0.5f, 0.36f));
            var dropValue = CreateRewardRow(panelGo.transform, "DropRow", "Drop", "+0", new Vector2(0.5f, 0.26f));

            Button closeButton = CreateButton(panelGo.transform, "ConfirmButton", "확인", new Vector2(0.5f, 0.1f), new Vector2(200f, 56f));

            OfflineSettlementUiEventAdapter adapter = EnsureAdapter();
            if (adapter == null)
                Debug.LogWarning("[OfflineSettlementUI] Adapter could not be auto-created. Assign CombatBootPresenter and add adapter manually.");

            var popup = panelGo.AddComponent<OfflineSettlementPopupPresenter>();
            popup.Configure(adapter, panelGo, closeButton, elapsedValue, goldValue, expValue, dropValue);

            Debug.Log("[OfflineSettlementUI] Sample popup created and wired.");
        }

        private void EnsureReferences()
        {
            if (targetCanvas == null)
                targetCanvas = GetComponentInParent<Canvas>();

            if (combatBootPresenter == null)
                combatBootPresenter = FindObjectOfType<CombatBootPresenter>(true);
        }

        private OfflineSettlementUiEventAdapter EnsureAdapter()
        {
            if (combatBootPresenter == null)
            {
                combatBootPresenter = FindObjectOfType<CombatBootPresenter>(true);
                if (combatBootPresenter == null)
                    return null;
            }

            var adapter = combatBootPresenter.GetComponent<OfflineSettlementUiEventAdapter>();
            if (adapter == null)
                adapter = combatBootPresenter.gameObject.AddComponent<OfflineSettlementUiEventAdapter>();

            return adapter;
        }

        private static GameObject CreateUiRoot(Transform parent, string objectName, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return go;
        }

        private static TMP_Text CreateRewardRow(Transform parent, string rowName, string label, string initialValue, Vector2 anchor)
        {
            var labelText = CreateText(parent, rowName + "Label", label, 24, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(labelText.rectTransform, anchor, anchor, new Vector2(0f, 0.5f), new Vector2(-180f, 0f), new Vector2(200f, 36f));

            var valueText = CreateText(parent, rowName + "Value", initialValue, 24, FontStyles.Normal, TextAlignmentOptions.Right);
            SetRect(valueText.rectTransform, anchor, anchor, new Vector2(1f, 0.5f), new Vector2(180f, 0f), new Vector2(220f, 36f));
            return valueText;
        }

        private static TMP_Text CreateText(Transform parent, string objectName, string text, int size, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, size);

            var image = go.GetComponent<Image>();
            image.color = new Color32(239, 191, 74, 255);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color32(239, 191, 74, 255);
            colors.highlightedColor = new Color32(255, 209, 95, 255);
            colors.pressedColor = new Color32(210, 165, 60, 255);
            button.colors = colors;

            var text = CreateText(go.transform, "Label", label, 24, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            text.color = new Color32(51, 35, 18, 255);

            return button;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
        }
    }
}
