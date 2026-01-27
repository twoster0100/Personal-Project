#if HAS_ADDRESSABLES
using System;
using MyGame.Application.Lifetime;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MyGame.Infrastructure.AddressablesAdapters
{
    /// <summary>
    /// Addressables 핸들을 AppLifetime에 묶어서,
    /// 스코프 종료(Dispose) 시 자동 Release 되도록 하는 어댑터.
    /// </summary>
    public static class AddressablesLifetime
    {
        private sealed class HandleReleaser : IDisposable
        {
            private AsyncOperationHandle _handle;
            private bool _hasHandle;

            public HandleReleaser(AsyncOperationHandle handle)
            {
                _handle = handle;
                _hasHandle = true;
            }

            public void Dispose()
            {
                if (!_hasHandle) return;

                try
                {
                    Addressables.Release(_handle);
                }
                finally
                {
                    _hasHandle = false;
                }
            }
        }

        public static void Register(AppLifetime lifetime, AsyncOperationHandle handle)
        {
            if (lifetime == null) throw new ArgumentNullException(nameof(lifetime));
            lifetime.Add(new HandleReleaser(handle));
        }
    }
}
#endif
