using UnityEngine;
using PartySelection.Model;

namespace PartySelection.Provider
{
    /// <summary>
    /// 테스트/연결 확인용 더미 Provider.
    /// - 나중에 실제 데이터 시스템으로 교체 대상
    /// - "코드가 정상 동작하는지" UI 단계에서 빠르게 확인하려고 둠
    /// </summary>
    public sealed class DummyStatsProvider : MonoBehaviour, IStatsProvider
    {
        [Header("Dummy Names")]
        [SerializeField] private string partyAName = "Party A";
        [SerializeField] private string partyBName = "Party B";
        [SerializeField] private string partyCName = "Party C";

        [Header("Dummy Portraits (optional)")]
        [SerializeField] private Sprite partyAPortrait;
        [SerializeField] private Sprite partyBPortrait;
        [SerializeField] private Sprite partyCPortrait;

        public CharacterProfile GetProfile(PartyType party, int slotIndex)
        {
            // slotIndex를 이용해 이름 변형 가능(예: Slot1 전용 캐릭터명)
            // 지금은 "파티 타입 + 슬롯 번호"만 보여줌
            return party switch
            {
                PartyType.A => new CharacterProfile($"{partyAName} - Slot {slotIndex + 1}", partyAPortrait),
                PartyType.B => new CharacterProfile($"{partyBName} - Slot {slotIndex + 1}", partyBPortrait),
                PartyType.C => new CharacterProfile($"{partyCName} - Slot {slotIndex + 1}", partyCPortrait),
                _ => new CharacterProfile($"Unknown - Slot {slotIndex + 1}", null)
            };
        }
    }
}
