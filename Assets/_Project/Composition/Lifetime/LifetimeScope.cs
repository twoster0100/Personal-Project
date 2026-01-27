using UnityEngine;
using MyGame.Application.Lifetime;
using MyGame.Composition;

namespace MyGame.Presentation.Lifetime
{
    public sealed class LifetimeScope : MonoBehaviour
    {
        public AppLifetime Lifetime { get; private set; }

        private void Awake()
        {
            Lifetime = new AppLifetime();

            // 앱 종료 시에도 정리되게 앱 Lifetime에 등록(이중 Dispose 안전)
            if (AppCompositionRoot.Instance != null)
                AppCompositionRoot.RegisterDisposable(Lifetime);
        }

        private void OnDestroy()
        {
            Lifetime?.Dispose();
            Lifetime = null;
        }
    }
}
