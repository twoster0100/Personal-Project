#if HAS_ADDRESSABLES
using System;
using System.Threading;
using System.Threading.Tasks;
using MyGame.Application.Assets;
using MyGame.Application.Diagnostics;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MyGame.Infrastructure.AddressablesAdapters
{
    public sealed class AddressablesAssetProvider : IAssetProvider
    {
        private sealed class AddressablesHandle<T> : IAssetHandle<T>
        {
            private AsyncOperationHandle<T> _handle;
            private bool _hasHandle;

            public AddressablesHandle(AsyncOperationHandle<T> handle)
            {
                _handle = handle;
                _hasHandle = true;
                ResourceAudit.AcquireAddressablesHandle();
            }

            public T Asset => _handle.Result;
            public bool IsValid => _hasHandle && _handle.IsValid();

            public void Dispose()
            {
                if (!_hasHandle) return;
                _hasHandle = false;

                if (_handle.IsValid())
                    Addressables.Release(_handle);

                ResourceAudit.ReleaseAddressablesHandle();
            }
        }

        public async Task<IAssetHandle<T>> LoadAsync<T>(string key, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Asset key is null or empty.", nameof(key));

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);

            try
            {
                T asset = await handle.Task;
                ct.ThrowIfCancellationRequested();
                return new AddressablesHandle<T>(handle);
            }
            catch
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
                throw;
            }
        }
    }
}
#else
using System;
using System.Threading;
using System.Threading.Tasks;
using MyGame.Application.Assets;

namespace MyGame.Infrastructure.AddressablesAdapters
{
    public sealed class AddressablesAssetProvider : IAssetProvider
    {
        public Task<IAssetHandle<T>> LoadAsync<T>(string key, CancellationToken ct = default)
        {
            return Task.FromException<IAssetHandle<T>>(
                new NotSupportedException("Addressables package not installed. Define HAS_ADDRESSABLES to enable."));
        }
    }
}
#endif
