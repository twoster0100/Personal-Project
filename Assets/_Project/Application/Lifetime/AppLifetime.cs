using System;
using System.Collections.Generic;
using System.Threading;

namespace MyGame.Application.Lifetime
{
    /// <summary>
    /// 앱 단위 수명 관리자.
    /// - 규약 5) Release/Dispose 규약 강제의 기반.
    /// - 이벤트 구독 해제, UniTask 취소, Tween Kill, Addressables Release 등을 "한 곳"에서 정리 가능.
    /// </summary>
    public sealed class AppLifetime : IDisposable
    {
        private readonly List<IDisposable> _disposables = new();
        private readonly CancellationTokenSource _cts = new();

        public CancellationToken Token => _cts.Token;
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 수명 종료 시 자동 Dispose 될 리소스를 등록.
        /// 이미 Dispose 된 수명에 Add하면 즉시 Dispose(누수 방지).
        /// </summary>
        public void Add(IDisposable disposable)
        {
            if (disposable == null) return;

            if (IsDisposed)
            {
                disposable.Dispose();
                return;
            }

            _disposables.Add(disposable);
        }

        /// <summary>
        /// 수명 종료 시 실행할 콜백 등록.
        /// </summary>
        public void Add(Action onDispose)
        {
            if (onDispose == null) return;
            Add(new ActionDisposable(onDispose));
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            // 1) 우선 취소 신호(비동기 루프 종료 유도)
            try { _cts.Cancel(); } catch { /* ignore */ }

            // 2) 등록된 리소스 정리(역순 권장)
            for (int i = _disposables.Count - 1; i >= 0; i--)
            {
                try { _disposables[i]?.Dispose(); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
            }

            _disposables.Clear();
            _cts.Dispose();
        }

        private sealed class ActionDisposable : IDisposable
        {
            private Action _action;
            public ActionDisposable(Action action) => _action = action;

            public void Dispose()
            {
                var a = _action;
                _action = null;
                a?.Invoke();
            }
        }
    }

    public static class LifetimeExtensions
    {
        /// <summary>
        /// disposable.AddTo(root.Lifetime) 패턴으로 등록 편의 제공.
        /// </summary>
        public static T AddTo<T>(this T disposable, AppLifetime lifetime) where T : IDisposable
        {
            lifetime?.Add(disposable);
            return disposable;
        }
    }
}
