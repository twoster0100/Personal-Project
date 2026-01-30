using UnityEngine;
using MyGame.Application.Save;

namespace MyGame.Presentation.Settings
{
    /// <summary>
    /// AutoModeController ↔ SettingsSaveData.autoMode 연결.
    /// </summary>
    public sealed class AutoModeSettingsBinding : MonoBehaviour, ISettingsBinding
    {
        [SerializeField] private AutoModeController autoMode;

        public void ApplyFromSave(SettingsSaveData data)
        {
            if (autoMode == null) return;
            autoMode.SetAuto(data.autoMode);
        }

        public void CaptureToSave(SettingsSaveData data)
        {
            if (autoMode == null) return;
            data.autoMode = autoMode.IsAuto;
        }
    }
}
