using UnityEngine;
using UnityEngine.Rendering;

public static class FrameSet
{
    // 게임 시작 시 1회 실행 (씬 로드 전에 적용)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        Application.targetFrameRate = 30;        // 30fps 목표

        // 프레임 간격(렌더링 빈도) - 30fps면 매 프레임 렌더
        OnDemandRendering.renderFrameInterval = 1;

        // 화면 꺼짐 방지
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}
