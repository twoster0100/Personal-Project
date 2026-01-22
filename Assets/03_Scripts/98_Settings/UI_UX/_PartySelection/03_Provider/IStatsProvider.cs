using UnityEngine;
using PartySelection.Model;

namespace PartySelection.Provider
{
    /// <summary>
    /// View에 보여줄 프로필 데이터 묶음.
    /// - DisplayName: 좌상단 이름
    /// - PortraitSprite: 좌상단 초상화(일반적으로 UI Image에 사용)
    /// </summary>
    public readonly struct CharacterProfile
    {
        public readonly string DisplayName;
        public readonly Sprite PortraitSprite;

        public CharacterProfile(string displayName, Sprite portraitSprite)
        {
            DisplayName = displayName;
            PortraitSprite = portraitSprite;
        }
    }

    /// <summary>
    /// DIP(의존성 역전):
    /// - Presenter는 "실제 캐릭터 DB"를 몰라도 됨
    /// - 오직 IStatsProvider에게 "이 슬롯/파티의 프로필 줘"만 요청
    /// - 나중에 진짜 데이터(플레이어 파티/캐릭터 성장/장비) 붙일 때
    ///   DummyStatsProvider를 실제 구현으로 교체하면 됨
    /// </summary>
    public interface IStatsProvider
    {
        CharacterProfile GetProfile(PartyType party, int slotIndex);
    }
}
