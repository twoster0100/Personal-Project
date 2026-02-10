using UnityEngine;

namespace MyGame.Composition
{
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

            //  LateFrameTick 단계 실행
            root.Ticks.DoLateFrame(Time.deltaTime);
        }
    }
}
