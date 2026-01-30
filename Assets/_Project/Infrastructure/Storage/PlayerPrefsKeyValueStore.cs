using MyGame.Application.Storage;
using UnityEngine;

namespace MyGame.Infrastructure.Storage
{
    public sealed class PlayerPrefsKeyValueStore : IKeyValueStore
    {
        public bool TryGetString(string key, out string value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetString(key);
                return true;
            }
            value = null;
            return false;
        }

        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Save() => PlayerPrefs.Save();
    }
}
