using System.Threading;
using System.Threading.Tasks;

namespace MyGame.Application.Auth
{
    public interface IAuthService
    {
        Task<AuthSession> SignInAsync(CancellationToken ct);
        AuthSession Current { get; }
        void SignOut();
    }

    public readonly struct AuthSession
    {
        public readonly string UserId;
        public readonly bool IsSignedIn;

        public AuthSession(string userId)
        {
            UserId = userId;
            IsSignedIn = !string.IsNullOrEmpty(userId);
        }
    }
}
