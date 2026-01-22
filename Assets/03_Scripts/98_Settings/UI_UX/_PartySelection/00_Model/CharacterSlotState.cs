using System;

namespace PartySelection.Model
{
    /// <summary>
    /// 현재 UI에서 선택된 "캐릭터 슬롯(0~3)" 상태만 관리.
    /// </summary>
    public sealed class CharacterSlotState
    {
        public int SelectedSlotIndex { get; private set; } = 0;

        public event Action<int> OnSelectedSlotChanged;

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4) return;
            if (SelectedSlotIndex == slotIndex) return;

            SelectedSlotIndex = slotIndex;
            OnSelectedSlotChanged?.Invoke(slotIndex);
        }
    }
}
