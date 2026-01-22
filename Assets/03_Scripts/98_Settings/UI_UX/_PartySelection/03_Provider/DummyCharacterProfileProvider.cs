using System.Collections.Generic;
using UnityEngine;

namespace PartySelection.Provider
{
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

        // ✅ 캐시: id -> Entry
        private Dictionary<string, Entry> _cache;

        private void Awake()
        {
            BuildCache();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 인스펙터 수정 시 캐시 갱신(에디터 편의)
            BuildCache();
        }
#endif

        private void BuildCache()
        {
            _cache = new Dictionary<string, Entry>(64);

            if (entries == null) return;

            foreach (var e in entries)
            {
                if (e == null) continue;
                if (string.IsNullOrEmpty(e.characterId)) continue;

                // 중복 ID 방지: 마지막 값으로 덮어쓰기(또는 Debug.LogWarning 가능)
                _cache[e.characterId] = e;
            }
        }

        public CharacterProfile GetProfile(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return new CharacterProfile("Empty", null);

            if (_cache != null && _cache.TryGetValue(characterId, out var e))
                return new CharacterProfile(e.displayName, e.portrait);

            return new CharacterProfile($"Unknown ({characterId})", null);
        }
    }
}
