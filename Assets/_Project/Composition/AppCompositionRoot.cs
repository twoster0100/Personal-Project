using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Application.Tick;
using MyGame.Application.Lifetime;
using MyGame.Infrastructure.FrameRate;

namespace MyGame.Composition
{
    public sealed class AppCompositionRoot : MonoBehaviour
    {
        public static AppCompositionRoot Instance { get; private set; }

        public TickScheduler Ticks { get; private set; }
        public SimulationClock SimulationClock { get; private set; }
        public IFrameRateService FrameRate { get; private set; }

        /// <summary>
        /// ✅ 앱 전체 수명(Dispose/취소 토대)
        /// </summary>
        public AppLifetime Lifetime { get; private set; }

        private static readonly List<object> _pendingTickables = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ✅ 조립은 여기 한 곳에서만
            Lifetime = new AppLifetime();

            Ticks = new TickScheduler();
            SimulationClock = new SimulationClock(Ticks, tickRate: 30, maxStepsPerFrame: 5);

            FrameRate = new UnityFrameRateService();
            FrameRate.SetMode(FrameRateMode.Idle30);

            gameObject.AddComponent<AppTickRunner>();

            // 대기 등록 처리
            for (int i = 0; i < _pendingTickables.Count; i++)
                Ticks.Register(_pendingTickables[i]);

            _pendingTickables.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            // ✅ 앱 종료/파괴 시 한 번에 정리
            Lifetime?.Dispose();
            Lifetime = null;

            _pendingTickables.Clear();
        }

        public static void RegisterWhenReady(object tickable)
        {
            if (tickable == null) return;

            var inst = Instance;
            if (inst != null)
            {
                inst.Ticks.Register(tickable);
                return;
            }

            if (!_pendingTickables.Contains(tickable))
                _pendingTickables.Add(tickable);
        }

        public static void UnregisterTickable(object tickable)
        {
            if (tickable == null) return;

            var inst = Instance;
            if (inst != null)
                inst.Ticks.Unregister(tickable);

            _pendingTickables.Remove(tickable);
        }

        /// <summary>
        /// ✅ 규약 5) Dispose 강제: 앱 수명 종료 시 자동 정리 등록
        /// </summary>
        public static void RegisterDisposable(IDisposable disposable)
        {
            var inst = Instance;
            if (inst == null)
            {
                // AppRoot 생성 전 등록 시도면 즉시 Dispose(누수 방지)
                disposable?.Dispose();
                return;
            }

            inst.Lifetime.Add(disposable);
        }

        public static void RegisterOnDispose(Action onDispose)
        {
            var inst = Instance;
            if (inst == null)
            {
                onDispose?.Invoke();
                return;
            }

            inst.Lifetime.Add(onDispose);
        }
    }

    public sealed class AppTickRunner : MonoBehaviour
    {
        private void Update()
        {
            var root = AppCompositionRoot.Instance;
            if (root == null) return;

            float dt = Time.deltaTime;

            // 1) 시뮬레이션(30Hz) 먼저
            root.SimulationClock.Advance(dt);

            // 2) 프레임 Tick
            root.Ticks.DoFrame(dt);

            // 3) 언스케일(연출/UI)
            root.Ticks.DoUnscaled(Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            var root = AppCompositionRoot.Instance;
            if (root == null) return;

            // ✅ LateFrameTick 단계가 있다면 여기서 실행 (B-4에서 이미 도입한 상태)
            root.Ticks.DoLateFrame(Time.deltaTime);
        }
    }
}
