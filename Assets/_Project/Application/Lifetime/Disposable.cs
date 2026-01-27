using System;

namespace MyGame.Application.Lifetime
{
    /// <summary>Dispose 시 콜백 실행</summary>
    public sealed class Disposable : IDisposable
    {
        private Action _onDispose;

        private Disposable(Action onDispose) => _onDispose = onDispose;

        public static IDisposable Create(Action onDispose) => new Disposable(onDispose);

        public void Dispose()
        {
            var a = _onDispose;
            _onDispose = null;
            a?.Invoke();
        }
    }
}
