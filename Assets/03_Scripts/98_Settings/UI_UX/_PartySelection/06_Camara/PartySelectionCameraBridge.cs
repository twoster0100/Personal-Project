using PartySelection.Installer;
using UnityEngine;
using Cinemachine;

namespace PartySelection.Camera
{
    /// <summary>
    /// UI(PartySelection) → 카메라(Cinemachine) 연결 브릿지.
    /// - Presenter는 카메라를 몰라야 SOLID(DIP) 유지됨.
    /// - Installer가 노출한 이벤트를 구독해서 카메라 Follow만 교체한다.
    /// </summary>
    public sealed class PartySelectionCameraBridge : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PartySelectionInstaller installer;

        [Header("Cinemachine")]
        [SerializeField] private CinemachineVirtualCamera vcamParty;

        [Header("Slot Targets (World Transforms)")]
        [Tooltip("슬롯(0~3)에 해당하는 '월드 캐릭터 Transform'을 드래그해서 넣어주세요.")]
        [SerializeField] private Transform[] slotTargets = new Transform[4];

        [Header("Options")]
        [Tooltip("선택 시 LookAt도 같이 설정하고 싶으면 체크")]
        [SerializeField] private bool setLookAtToo = false;

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
            if (vcamParty == null)
            {
                Debug.LogWarning("[PartySelectionCameraBridge] vcamParty is missing.");
                return;
            }

            if (slotTargets == null || slotTargets.Length != 4)
            {
                Debug.LogWarning("[PartySelectionCameraBridge] slotTargets size must be 4.");
                return;
            }

            var target = slotTargets[slotIndex];
            if (target == null)
            {
                Debug.LogWarning($"[PartySelectionCameraBridge] slotTargets[{slotIndex}] is null. (id={characterId})");
                return;
            }

            // ✅ Follow 전환
            vcamParty.Follow = target;

            // 옵션: LookAt도 같이
            if (setLookAtToo)
                vcamParty.LookAt = target;

            // “즉시 안정화”(선택 직후 순간 튐 방지)
            vcamParty.PreviousStateIsValid = false;
        }
    }
}
