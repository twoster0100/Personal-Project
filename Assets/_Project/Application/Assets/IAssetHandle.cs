using System;

namespace MyGame.Application.Assets
{
    public interface IAssetHandle<out T> : IDisposable
    {
        T Asset { get; }
        bool IsValid { get; }
    }
}
