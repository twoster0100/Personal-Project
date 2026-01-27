using System;
using DG.Tweening;
using MyGame.Application.Lifetime;

namespace MyGame.Infrastructure.DOTweenAdapters
{
    public static class DOTweenLifetime
    {
        private sealed class TweenKiller : IDisposable
        {
            private Tween _tween;

            public TweenKiller(Tween tween)
            {
                _tween = tween;
                // 외부에서 Kill 되어도 이쪽이 안전하게 종료되도록
                _tween?.OnKill(() => _tween = null);
            }

            public void Dispose()
            {
                if (_tween == null) return;

                // active 체크는 DOTween 버전에 따라 다르니 Kill만 호출해도 안전하게 처리됨
                _tween.Kill();
                _tween = null;
            }
        }

        /// <summary>수명 종료 시 Tween 자동 Kill</summary>
        public static Tween KillOnDispose(this Tween tween, AppLifetime lifetime)
        {
            if (tween == null || lifetime == null) return tween;
            lifetime.Add(new TweenKiller(tween));
            return tween;
        }
    }
}
