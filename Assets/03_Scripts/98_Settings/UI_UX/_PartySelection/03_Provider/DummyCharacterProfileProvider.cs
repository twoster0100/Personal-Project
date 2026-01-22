using UnityEngine;

namespace PartySelection.Provider
{
    /// <summary>
    /// 테스트용: ID로 프로필을 제공하는 더미 Provider.
    /// - 나중에 실제 캐릭터 데이터(SO/DB/세이브)로 교체
    /// </summary>
    public sealed class DummyCharacterProfileProvider : MonoBehaviour, ICharacterProfileProvider
    {
        [System.Serializable]
        public sealed class Entry
        {
            public string characterId;
            public string displayName;
            public Sprite portrait;
        }

        [SerializeField] private Entry[] entries;

        public CharacterProfile GetProfile(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return new CharacterProfile("Empty", null);

            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    if (e != null && e.characterId == characterId)
                        return new CharacterProfile(e.displayName, e.portrait);
                }
            }

            return new CharacterProfile($"Unknown ({characterId})", null);
        }
    }
}
