using UnityEngine;

namespace PartySelection.Provider
{
    /// <summary>
    /// View에 보여줄 프로필 데이터.
    /// </summary>
    public readonly struct CharacterProfile
    {
        public readonly string DisplayName;
        public readonly Sprite Portrait;

        public CharacterProfile(string displayName, Sprite portrait)
        {
            DisplayName = displayName;
            Portrait = portrait;
        }
    }

    /// <summary>
    /// DIP: Presenter는 "캐릭터 DB"를 몰라도 된다.
    /// CharacterId만 던지면 프로필을 돌려주는 공급자.
    /// </summary>
    public interface ICharacterProfileProvider
    {
        CharacterProfile GetProfile(string characterId);
    }
}
