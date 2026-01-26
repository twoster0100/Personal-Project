using UnityEngine;
using UnityEngine.Rendering;

namespace MyGame.Infrastructure.FrameRate
{
    public sealed class UnityFrameRateService : IFrameRateService
    {
        public FrameRateMode Current { get; private set; }

        public void SetMode(FrameRateMode mode)
        {
            if (Current == mode) return;
            Current = mode;

            // 모바일에서는 vSync를 끄고 targetFrameRate로 제어하는 게 예측 가능
            QualitySettings.vSyncCount = 0;

            // ✅ 여기 핵심: UnityEngine.Application을 전역으로 명시
            global::UnityEngine.Application.targetFrameRate =
                (mode == FrameRateMode.Idle30) ? 30 : 60;

            // (옵션) 필요 시 렌더 간격 조정. 지금은 1로 고정
            OnDemandRendering.renderFrameInterval = 1;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
