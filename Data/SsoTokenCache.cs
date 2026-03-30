using System.Collections.Concurrent;

namespace QIN_Production_Web.Data
{
    public class SsoTokenCache
    {
        private readonly ConcurrentDictionary<Guid, (UserSession Session, DateTime Expiry)> _cache = new();

        public Guid StoreSession(UserSession session)
        {
            // Clean up expired tokens occasionally
            CleanUp();

            var token = Guid.NewGuid();
            // Tokens are valid for exactly 60 seconds.
            _cache[token] = (session, DateTime.UtcNow.AddSeconds(60));
            return token;
        }

        public UserSession? RetrieveSession(Guid token)
        {
            if (_cache.TryRemove(token, out var entry))
            {
                if (entry.Expiry >= DateTime.UtcNow)
                {
                    return entry.Session;
                }
            }
            return null;
        }

        private void CleanUp()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cache.Where(k => k.Value.Expiry < now).Select(k => k.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}
