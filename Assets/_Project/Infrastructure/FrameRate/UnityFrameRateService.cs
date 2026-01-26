using UnityEngine;
using UnityEngine.Rendering;

namespace MyGame.Infrastructure.FrameRate
{
    public sealed class UnityFrameRateService : IFrameRateService
    {
        public FrameRateMode Current { get; private set; }

        public void SetMode(FrameRateMode mode)
        {
            Current = mode;

            int desired = (mode == FrameRateMode.Idle30) ? 30 : 60;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            OnDemandRendering.renderFrameInterval = 1;

            // 에디터에서도 모바일처럼 동작하게: vSync 끄고 targetFrameRate로 상한 고정
            QualitySettings.vSyncCount = 0;
            global::UnityEngine.Application.targetFrameRate = desired;
        }
    }
}
