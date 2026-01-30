
namespace MyGame.Application.Storage
{
    public interface IKeyValueStore
    {
        bool TryGetString(string key, out string value);
        void SetString(string key, string value);
        void Save(); // 필요 시 (PlayerPrefs.Save 같은)
    }
}
