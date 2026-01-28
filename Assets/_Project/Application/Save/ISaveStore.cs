using System.Threading;
using System.Threading.Tasks;

namespace MyGame.Application.Save
{
    public readonly struct SaveReadResult
    {
        public readonly bool Found;
        public readonly string Contents;

        public SaveReadResult(bool found, string contents)
        {
            Found = found;
            Contents = contents;
        }
    }

    /// <summary>
    /// ✅ Port: 저장 매체 추상화
    /// - 로컬 JSON이든, 나중 CloudSave든 동일 인터페이스로 교체
    /// </summary>
    public interface ISaveStore
    {
        Task WriteAsync(string key, string contents, CancellationToken ct = default);
        Task<SaveReadResult> ReadAsync(string key, CancellationToken ct = default);
        Task DeleteAsync(string key, CancellationToken ct = default);
    }
}
