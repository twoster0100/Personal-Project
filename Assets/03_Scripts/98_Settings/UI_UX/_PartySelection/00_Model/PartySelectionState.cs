using System;

namespace PartySelection.Model
{
    /// <summary>
    /// "현재 선택된 슬롯" 상태(State)만 관리.
    /// - Presenter가 슬롯 클릭 이벤트를 받으면 여기 SelectSlot() 호출
    /// - 상태 변경 시 OnSelectedSlotChanged 이벤트 발생
    /// - View는 상태를 직접 바꾸지 않음(흐름 통제는 Presenter)
    /// </summary>
    public sealed class PartySelectionState
    {
        public int SelectedSlotIndex { get; private set; } = 0;

        /// <summary>
        /// 슬롯 선택이 실제로 바뀌었을 때만 호출됨.
        /// </summary>
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
