using UnityEngine;
using MyGame.Application.Tick;
using MyGame.Infrastructure.FrameRate;
using System.Collections.Generic;

namespace MyGame.Composition
{
    public sealed class AppCompositionRoot : MonoBehaviour
    {
        public static AppCompositionRoot Instance { get; private set; }

        public TickScheduler Ticks { get; private set; }
        public SimulationClock SimulationClock { get; private set; }
        public IFrameRateService FrameRate { get; private set; }

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
            Ticks = new TickScheduler();
            SimulationClock = new SimulationClock(Ticks, tickRate: 30, maxStepsPerFrame: 5);

            FrameRate = new UnityFrameRateService();
            FrameRate.SetMode(FrameRateMode.Idle30);

            gameObject.AddComponent<AppTickRunner>();

            for (int i = 0; i < _pendingTickables.Count; i++)
                Ticks.Register(_pendingTickables[i]);

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
    }

    public sealed class AppTickRunner : MonoBehaviour
    {
        private void Update()
        {
            var root = AppCompositionRoot.Instance;
            if (root == null) return;

            float dt = Time.deltaTime;

            // ✅ 1) 시뮬레이션(30Hz)을 먼저 돌려서
            // CombatController가 AutoMoveVector 같은 "의도"를 먼저 만들어둔다.
            root.SimulationClock.Advance(dt);

            // ✅ 2) 그 다음 프레임 Tick에서 이동/입력/표현 반영
            root.Ticks.DoFrame(dt);

            // ✅ 3) LateUpdate 대체: 이동 이후 계산(예: 애니 speed 계산)
            root.Ticks.DoLateFrame(dt);

            // ✅ 4) UI/연출 전용(시간정지에서도 진행)
            root.Ticks.DoUnscaled(Time.unscaledDeltaTime);
        }
    }
}
