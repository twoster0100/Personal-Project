using System;
using UnityEngine;
using MyGame.Application;
using MyGame.Application.Assets;
using MyGame.Application.Auth;
using MyGame.Application.Storage;
using MyGame.Application.Tick;
using MyGame.Application.Lifetime;
using MyGame.Application.Save;
using MyGame.Infrastructure.AddressablesAdapters;
using MyGame.Infrastructure.Auth;
using MyGame.Infrastructure.FrameRate;
using MyGame.Infrastructure.Save;
using MyGame.Infrastructure.Storage;

namespace MyGame.Composition
{
    public sealed class AppCompositionRoot : MonoBehaviour
    {
        public static AppCompositionRoot Instance { get; private set; }

        public TickScheduler Ticks { get; private set; }
        public SimulationClock SimulationClock { get; private set; }
        public IFrameRateService FrameRate { get; private set; }

        /// <summary>✅ 앱 전체 수명(Dispose/취소 토대)</summary>
        public AppLifetime Lifetime { get; private set; }

        public SaveService Save { get; private set; }

        /// <summary>✅ Auth (게스트 로그인 → 추후 UGS/Auth로 교체 가능)</summary>
        public IAuthService Auth { get; private set; }
        public IAssetProvider Assets { get; private set; }

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
            // ✅ Auth 조립 (얇은 게스트)
            // ----------------------------
            IKeyValueStore kvStore = new PlayerPrefsKeyValueStore();
            Auth = new GuestAuthService(kvStore);

            // ----------------------------
            // ✅ Save 조립 (Version/Migration 프레임 포함)
            // ----------------------------
            ISaveStore saveStore = new JsonFileSaveStore(subFolder: "Saves");
            ISaveCodec codec = new UnityJsonSaveCodec();

            Save = new SaveService(
                saveStore,
                codec,
                currentSchemaVersion: PrototypeSaveData.SchemaVersion,
                migrations: null
            );

            // ----------------------------
            // ✅ Asset Provider 조립 (Addressables 흡수)
            // ----------------------------
            Assets = new AddressablesAssetProvider();

            // ✅ Application 계층(App) 초기화: 다른 코드가 Composition을 참조하지 않게 만든다
            App.Initialize(Ticks, Lifetime, Save, Auth, Assets);

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
            Auth = null;
            Assets = null;
        }
    }
}
