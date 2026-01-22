using PartySelection.Installer;
using UnityEngine;

namespace PartySelection.Camera
{
    /// <summary>
    /// UI(Installer 이벤트) → CameraDirector 명령 전달 브릿지.
    /// </summary>
    public sealed class PartySelectionCameraBridge : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PartySelectionInstaller installer;

        [Header("Camera Director (single owner of FollowProxy)")]
        [SerializeField] private PartyCameraDirectorSwoosh cameraDirector;

        private void OnEnable()
        {
            if (installer != null)
                installer.OnSelectedSlotChanged += HandleSelectedSlotChanged;
        }

        private void OnDisable()
        {
            if (installer != null)
                installer.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
        }

        private void HandleSelectedSlotChanged(int slotIndex, string characterId)
        {
            if (cameraDirector == null) return;
            cameraDirector.FocusSlot(slotIndex);
        }
    }
}
