using System;
using System.Threading;
using System.Threading.Tasks;
using MyGame.Application.Auth;
using MyGame.Application.Storage;

namespace MyGame.Infrastructure.Auth
{
    public sealed class GuestAuthService : IAuthService
    {
        private const string GuestIdKey = "auth.guestId";
        private readonly IKeyValueStore _store;

        public AuthSession Current { get; private set; }

        public GuestAuthService(IKeyValueStore store)
        {
            _store = store;
            Current = default;
        }

        public Task<AuthSession> SignInAsync(CancellationToken ct)
        {
            if (!_store.TryGetString(GuestIdKey, out var id) || string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                _store.SetString(GuestIdKey, id);
                _store.Save();
            }

            Current = new AuthSession(id);
            return Task.FromResult(Current);
        }

        public void SignOut()
        {
            Current = default;
        }
    }
}
