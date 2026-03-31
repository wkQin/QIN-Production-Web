using System.Collections.Concurrent;

namespace QIN_Production_Web.Data
{
    public class LoginTokenCache
    {
        private readonly ConcurrentDictionary<string, (UserSession Session, bool RememberMe, DateTime Expiration)> _tokens = new();

        public string GenerateToken(UserSession session, bool rememberMe)
        {
            var token = Guid.NewGuid().ToString("N");
            _tokens[token] = (session, rememberMe, DateTime.UtcNow.AddMinutes(1)); // Valid for 1 min
            
            // Cleanup old tokens
            foreach (var kvp in _tokens)
            {
                if (kvp.Value.Expiration < DateTime.UtcNow)
                {
                    _tokens.TryRemove(kvp.Key, out _);
                }
            }
            
            return token;
        }

        public bool TryGetToken(string token, out UserSession? session, out bool rememberMe)
        {
            if (_tokens.TryRemove(token, out var data) && data.Expiration >= DateTime.UtcNow)
            {
                session = data.Session;
                rememberMe = data.RememberMe;
                return true;
            }

            session = null;
            rememberMe = false;
            return false;
        }
    }
}
