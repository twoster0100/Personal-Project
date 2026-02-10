using System.Threading;
using System.Threading.Tasks;

namespace MyGame.Application.Assets
{
    /// <summary>
    /// ✅ Port: Addressables 등 외부 에셋 로딩을 Application에서 추상화.
    /// - Infrastructure에서 실제 구현(Addressables/Resources 등)
    /// </summary>
    public interface IAssetProvider
    {
        Task<IAssetHandle<T>> LoadAsync<T>(string key, CancellationToken ct = default);
    }
}
