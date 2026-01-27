using System;
using System.Collections.Generic;
using MyGame.Application.Lifetime;
using MyGame.Application.Tick;

namespace MyGame.Application
{
    /// <summary>
    /// ✅ 앱 전역 접점(Composition을 직접 참조하지 않기 위한 최소 Service Locator)
    /// - Tick 등록/해제, Dispose 등록을 Application 계층에서 제공
    /// - Initialize/Shutdown은 internal로 막고, Composition 어셈블리만 호출 가능(InternalsVisibleTo)
    /// </summary>
    public static class App
    {
        private static TickScheduler _ticks;
        private static AppLifetime _lifetime;

        private static bool _isShuttingDown;
        private static readonly List<object> _pendingTickables = new();

        public static bool IsInitialized => _ticks != null && !_isShuttingDown;

        internal static void Initialize(TickScheduler ticks, AppLifetime lifetime)
        {
            if (ticks == null) throw new ArgumentNullException(nameof(ticks));
            if (lifetime == null) throw new ArgumentNullException(nameof(lifetime));

            // 중복 초기화 방지
            if (_ticks != null) return;

            _isShuttingDown = false;
            _ticks = ticks;
            _lifetime = lifetime;

            // ✅ AppRoot 생성 전 등록된 Tickable을 한 번에 등록
            for (int i = 0; i < _pendingTickables.Count; i++)
                _ticks.Register(_pendingTickables[i]);

            _pendingTickables.Clear();
        }

        internal static void Shutdown()
        {
            _isShuttingDown = true;

            _ticks = null;
            _lifetime = null;

            _pendingTickables.Clear();
        }

        /// <summary>
        /// ✅ AppRoot가 아직 없을 수도 있으니 "대기 등록" 지원
        /// </summary>
        public static void RegisterWhenReady(object tickable)
        {
            if (tickable == null) return;
            if (_isShuttingDown) return;

            var ticks = _ticks;
            if (ticks != null)
            {
                ticks.Register(tickable);
                return;
            }

            if (!_pendingTickables.Contains(tickable))
                _pendingTickables.Add(tickable);
        }

        public static void UnregisterTickable(object tickable)
        {
            if (tickable == null) return;

            var ticks = _ticks;
            if (ticks != null)
                ticks.Unregister(tickable);

            _pendingTickables.Remove(tickable);
        }

        /// <summary>
        /// ✅ 규약 5) Dispose 강제: 앱 수명 종료 시 자동 정리 등록
        /// </summary>
        public static void RegisterDisposable(IDisposable disposable)
        {
            if (disposable == null) return;

            var lifetime = _lifetime;
            if (lifetime == null || _isShuttingDown)
            {
                // AppRoot 생성 전/종료 중 등록 시도면 즉시 Dispose(누수 방지)
                disposable.Dispose();
                return;
            }

            lifetime.Add(disposable);
        }

        public static void RegisterOnDispose(Action onDispose)
        {
            if (onDispose == null) return;

            var lifetime = _lifetime;
            if (lifetime == null || _isShuttingDown)
            {
                onDispose.Invoke();
                return;
            }

            lifetime.Add(onDispose);
        }
    }
}
