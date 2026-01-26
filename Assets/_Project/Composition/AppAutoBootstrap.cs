using UnityEngine;

namespace MyGame.Composition
{
    /// <summary>
    /// 어떤 씬에서 Play를 눌러도 AppRoot(Composition Root)가 보장되도록 자동 부트스트랩.
    /// - 규약: Composition Root 1곳 고정 유지
    /// - 씬마다 AppRoot를 심지 않아도 됨
    /// </summary>
    public static class AppAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureAppRoot()
        {
            if (AppCompositionRoot.Instance != null) return;

            var go = new GameObject("AppRoot");
            go.AddComponent<AppCompositionRoot>(); // Awake에서 DontDestroyOnLoad + TickRunner 세팅
        }
    }
}
