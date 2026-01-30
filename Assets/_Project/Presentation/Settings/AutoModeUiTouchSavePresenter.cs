using UnityEngine;
using UnityEngine.UI;

namespace MyGame.Presentation.Settings
{
    /// <summary>
    /// UI 입력(터치/클릭) 1회 = "변경 확정"으로 보고 저장을 트리거한다.
    /// - View(슬라이드/텍스트)는 시각만 담당
    /// - Model(AutoModeController) 변경 + SavePresenter에 Dirty 통지
    /// </summary>
    public sealed class AutoModeUiTouchSavePresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private AutoModeController autoMode;
        [SerializeField] private AutoModeSavePresenter savePresenter;

        [Header("UI")]
        [SerializeField] private Button toggleButton;

        [Header("Debug")]
        [SerializeField] private bool log = false;

        private void Reset()
        {
            toggleButton = GetComponent<Button>();
        }

        private void Awake()
        {
            if (!toggleButton) toggleButton = GetComponent<Button>();
            if (!toggleButton)
            {
                Debug.LogError("[AutoModeUi] Button is not assigned.");
                enabled = false;
                return;
            }

            toggleButton.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (autoMode == null)
            {
                Debug.LogError("[AutoModeUi] AutoModeController is not assigned.");
                return;
            }

            // 1) 모델 변경 (UI가 누른 그 순간 확정)
            autoMode.ToggleAuto();

            // 2) 저장 트리거 (이벤트 도착에 의존하지 않음)
            if (savePresenter != null)
                savePresenter.NotifyChangedFromUi();

            if (log)
                Debug.Log($"[AutoModeUi] Click -> IsAuto={autoMode.IsAuto}");
        }
    }
}
