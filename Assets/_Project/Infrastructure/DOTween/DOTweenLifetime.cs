using System;
using DG.Tweening;
using MyGame.Application.Diagnostics;
using MyGame.Application.Lifetime;

namespace MyGame.Infrastructure.DOTweenAdapters
{
    /// <summary>
    /// DOTween Tween을 AppLifetime에 묶어서 스코프 종료(Dispose) 시 Kill 되도록 하는 어댑터.
    /// + (개발용) ResourceAudit 카운터로 누수 여부를 빠르게 확인
    /// </summary>
    public static class DOTweenLifetime
    {
        private sealed class TweenKiller : IDisposable
        {
            private Tween _tween;
            private bool _disposed;
            private int _auditReleased; // 0 = not yet, 1 = released

            public TweenKiller(Tween tween)
            {
                _tween = tween;

                // ✅ Tween 획득 카운트
                ResourceAudit.AcquireTween();

                // 외부에서 Kill 되더라도 카운트가 내려가도록 onKill 체인
                if (_tween != null)
                {
                    var prev = _tween.onKill;
                    _tween.onKill = () =>
                    {
                        try { prev?.Invoke(); }
                        finally { ReleaseAuditOnce(); }
                    };
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                try
                {
                    if (_tween != null && _tween.active)
                        _tween.Kill();
                }
                finally
                {
                    // Kill이 이미 호출/완료됐든 아니든 1회만 내려가게 보장
                    ReleaseAuditOnce();
                    _tween = null;
                }
            }

            private void ReleaseAuditOnce()
            {
                if (System.Threading.Interlocked.Exchange(ref _auditReleased, 1) == 0)
                    ResourceAudit.ReleaseTween();
            }
        }

        public static void KillOnDispose(AppLifetime lifetime, Tween tween)
        {
            if (lifetime == null) throw new ArgumentNullException(nameof(lifetime));
            if (tween == null) return;
            lifetime.Add(new TweenKiller(tween));
        }
    }
}
