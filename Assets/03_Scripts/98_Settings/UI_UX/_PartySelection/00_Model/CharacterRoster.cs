using System;

namespace PartySelection.Model
{
    /// <summary>
    /// 슬롯(0~3)에 배치된 캐릭터 ID를 보관.
    /// - "12개 보유 캐릭터 목록"은 Provider(카탈로그/DB) 역할이고,
    /// - Roster는 "현재 4슬롯에 무엇이 배치됐는지"만 관리한다.
    /// </summary>
    [Serializable]
    public sealed class CharacterRoster
    {
        public const int SlotCount = 4;

        private readonly string[] _characterIds = new string[SlotCount];

        /// <summary>
        /// 슬롯의 캐릭터 ID가 변경될 때 발생.
        /// (카메라/전투/스폰 등 외부 시스템이 구독해 확장 가능)
        /// </summary>
        public event Action<int, string> OnSlotCharacterChanged;

        public string GetCharacterId(int slotIndex)
        {
            ValidateSlot(slotIndex);
            return _characterIds[slotIndex];
        }

        public void SetCharacterId(int slotIndex, string characterId)
        {
            ValidateSlot(slotIndex);

            if (_characterIds[slotIndex] == characterId)
                return;

            _characterIds[slotIndex] = characterId;
            OnSlotCharacterChanged?.Invoke(slotIndex, characterId);
        }

        private static void ValidateSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "slotIndex must be 0~3");
        }
    }
}
