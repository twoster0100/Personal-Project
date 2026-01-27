using System.Threading;

namespace MyGame.Application.Diagnostics
{
    /// <summary>
    /// 개발/검증용 "리소스 누수 감사(카운터)".
    /// - Release 빌드에서는 자동으로 비활성(0 반환, no-op)
    /// </summary>
    public static class ResourceAudit
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static int _addressablesHandles;
        private static int _tweens;

        public static int ActiveAddressablesHandles => Volatile.Read(ref _addressablesHandles);
        public static int ActiveTweens => Volatile.Read(ref _tweens);

        public static void AcquireAddressablesHandle() => Interlocked.Increment(ref _addressablesHandles);
        public static void ReleaseAddressablesHandle() => Interlocked.Decrement(ref _addressablesHandles);

        public static void AcquireTween() => Interlocked.Increment(ref _tweens);
        public static void ReleaseTween() => Interlocked.Decrement(ref _tweens);

        public static void ResetAll()
        {
            Interlocked.Exchange(ref _addressablesHandles, 0);
            Interlocked.Exchange(ref _tweens, 0);
        }

        public static string BuildReport(string reason)
        {
            return $"[ResourceAudit:{reason}] Addressables={ActiveAddressablesHandles}, Tweens={ActiveTweens}";
        }
#else
        public static int ActiveAddressablesHandles => 0;
        public static int ActiveTweens => 0;

        public static void AcquireAddressablesHandle() { }
        public static void ReleaseAddressablesHandle() { }
        public static void AcquireTween() { }
        public static void ReleaseTween() { }

        public static void ResetAll() { }
        public static string BuildReport(string reason) => $"[ResourceAudit:{reason}] (disabled)";
#endif
    }
}
