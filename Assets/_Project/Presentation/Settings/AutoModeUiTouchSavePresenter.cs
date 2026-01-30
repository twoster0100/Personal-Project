using UnityEngine;
using UnityEngine.UI;

namespace MyGame.Presentation.Settings
{
    /// <summary>
    /// Auto 토글 버튼을 눌렀을 때:
    /// 1) 모델 값 변경
    /// 2) SettingsSavePresenter에 "저장 필요"만 알림
    /// </summary>
    public sealed class AutoModeUiTouchSavePresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private AutoModeController autoMode;
        [SerializeField] private SettingsSavePresenter settingsSave;
        [SerializeField] private Button toggleButton;

        [Header("Debug")]
        [SerializeField] private bool log = false;

        private void Reset()
        {
            toggleButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (toggleButton == null)
            {
                Debug.LogError("[AutoModeUi] Toggle Button is not assigned.");
                return;
            }

            toggleButton.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (autoMode == null || settingsSave == null) return;

            autoMode.ToggleAuto();

            if (log)
                Debug.Log($"[AutoModeUi] Click -> IsAuto={autoMode.IsAuto}");

            settingsSave.NotifyChangedFromUi("AutoMode");
        }
    }
}
