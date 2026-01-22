using System;

namespace PartySelection.Model
{
    /// <summary>
    /// 슬롯(1~4)에 어떤 파티(A/B/C)가 배정돼 있는지 저장하는 Model.
    /// - 저장만 책임
    /// - UI, 애니메이션, 외부 데이터(스탯 등)와 분리
    /// - 나중에 세이브/로드가 붙어도 이 클래스는 유지되기 쉬움
    /// </summary>
    [Serializable]
    public sealed class PartyRoster
    {
        // 슬롯 인덱스: 0~3 (UI에서는 1~4로 보일 수 있음)
        private readonly PartyType[] _slotParty = new PartyType[4];

        public PartyType GetPartyAt(int slotIndex)
        {
            ValidateSlot(slotIndex);
            return _slotParty[slotIndex];
        }

        public void SetPartyAt(int slotIndex, PartyType party)
        {
            ValidateSlot(slotIndex);
            _slotParty[slotIndex] = party;
        }

        private static void ValidateSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "slotIndex must be 0~3");
        }
    }
}
