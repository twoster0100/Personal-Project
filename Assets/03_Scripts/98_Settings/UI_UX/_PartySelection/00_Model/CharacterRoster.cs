using System;

namespace PartySelection.Model
{
    /// <summary>
    /// 슬롯(0~3)에 배치된 캐릭터 ID를 보관.
    /// - 확장성 포인트: 나중에 "슬롯 교체"는 여기 ID만 바꾸면 됨.
    /// </summary>
    [Serializable]
    public sealed class CharacterRoster
    {
        private readonly string[] _characterIds = new string[4];

        public string GetCharacterId(int slotIndex)
        {
            ValidateSlot(slotIndex);
            return _characterIds[slotIndex];
        }

        public void SetCharacterId(int slotIndex, string characterId)
        {
            ValidateSlot(slotIndex);
            _characterIds[slotIndex] = characterId;
        }

        private static void ValidateSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "slotIndex must be 0~3");
        }
    }
}
