using System;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Tick;
using MyGame.Application.Lifetime;
using MyGame.Application.Save;
using MyGame.Infrastructure.FrameRate;
using MyGame.Infrastructure.Save;

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

        public SaveService Save { get; private set; }

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

            // ----------------------------
            // ✅ Save 조립 (Version/Migration 프레임 포함)
            // ----------------------------
            ISaveStore store = new JsonFileSaveStore(subFolder: "Saves");
            ISaveCodec codec = new UnityJsonSaveCodec();

            // 지금은 마이그레이션 비어도 OK (프레임만 깔기)
            Save = new SaveService(
                store,
                codec,
                currentSchemaVersion: PrototypeSaveData.SchemaVersion,
                migrations: null
            );

            // ✅ Application 계층(App) 초기화: 다른 코드가 Composition을 참조하지 않게 만든다
            App.Initialize(Ticks, Lifetime, Save);

            gameObject.AddComponent<AppTickRunner>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            // ✅ 외부 등록 차단(종료 중 pending 누수 방지)
            App.Shutdown();

            // ✅ 앱 종료/파괴 시 한 번에 정리
            Lifetime?.Dispose();
            Lifetime = null;

            Save = null;
        }

        // ----------------------------
        // (선택) 하위 호환: 기존 코드가 아직 AppCompositionRoot를 부르면 App으로 포워딩
        // ----------------------------
        [Obsolete("Use MyGame.Application.App.RegisterWhenReady(...) instead.")]
        public static void RegisterWhenReady(object tickable) => App.RegisterWhenReady(tickable);

        [Obsolete("Use MyGame.Application.App.UnregisterTickable(...) instead.")]
        public static void UnregisterTickable(object tickable) => App.UnregisterTickable(tickable);

        [Obsolete("Use MyGame.Application.App.RegisterDisposable(...) instead.")]
        public static void RegisterDisposable(IDisposable disposable) => App.RegisterDisposable(disposable);

        [Obsolete("Use MyGame.Application.App.RegisterOnDispose(...) instead.")]
        public static void RegisterOnDispose(Action onDispose) => App.RegisterOnDispose(onDispose);
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

            // ✅ LateFrameTick 단계 실행
            root.Ticks.DoLateFrame(Time.deltaTime);
        }
    }
}
