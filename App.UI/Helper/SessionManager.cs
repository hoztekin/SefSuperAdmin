using App.UI.DTOS;
using System.Text.Json;

namespace App.UI.Helper
{
    public class SessionManager
    {
        private const string TOKEN_FILE = "token.dat";
        private const string AGENT_FILE = "agent.enc";
        private static string _cachedAccessToken;
        private static string _cachedRefreshToken;
        private static string _userId;
        private static DateTime? _accessTokenExpiration;
        private static readonly object _lock = new object();
        private static List<string> _cachedRoles;

        public class SessionInfo
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public string UserId { get; set; }
            public DateTime AccessTokenExpiration { get; set; }
            public UserInfoDto UserInfo { get; set; }
            public List<string> Roles { get; set; }
            public List<string> Permissions { get; set; }
        }

        public static void SaveSession(string accessToken, string userId, DateTime accessTokenExpiration, List<string> roles, string refreshToken = null)
        {
            lock (_lock)
            {
                // Belleğe kaydet - burada property isimlerini düzelttik
                _cachedAccessToken = accessToken;
                _cachedRefreshToken = refreshToken;
                _userId = userId ?? "";
                // DateTime'ı her zaman UTC olarak saklayın
                _accessTokenExpiration = accessTokenExpiration.ToUniversalTime();
                _cachedRoles = roles ?? new List<string>();

                // Dosyaya kaydet (şifreleme olmadan deneyin önce)
                var data = new SessionInfo
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = userId,
                    AccessTokenExpiration = accessTokenExpiration.ToUniversalTime(),
                    Roles = roles
                };

                try
                {
                    var json = JsonSerializer.Serialize(data);
                    // Klasörün var olduğundan emin ol
                    var directory = Path.GetDirectoryName(GetTokenFilePath());
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllText(GetTokenFilePath(), json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Token kaydetme hatası: {ex.Message}");
                }
            }
        }

        public static SessionInfo GetSession()
        {
            lock (_lock)
            {
                // Önce bellekten kontrol et
                if (_cachedAccessToken != null && _accessTokenExpiration.HasValue)
                {
                    if (IsTokenValid())
                    {
                        return new SessionInfo
                        {
                            AccessToken = _cachedAccessToken,
                            RefreshToken = _cachedRefreshToken,
                            UserId = _userId,
                            AccessTokenExpiration = _accessTokenExpiration.Value,
                            Roles = _cachedRoles
                        };
                    }
                }

                // Dosyadan oku
                try
                {
                    if (File.Exists(GetTokenFilePath()))
                    {
                        var json = File.ReadAllText(GetTokenFilePath());
                        var session = JsonSerializer.Deserialize<SessionInfo>(json);

                        // Belleğe al
                        _cachedAccessToken = session.AccessToken;
                        _cachedRefreshToken = session.RefreshToken;
                        _userId = session.UserId;
                        _accessTokenExpiration = session.AccessTokenExpiration;
                        _cachedRoles = session.Roles;

                        if (IsTokenValid())
                        {
                            return session;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Token okuma hatası: {ex.Message}");
                }

                return null;
            }
        }



        public static bool IsTokenValid()
        {
            return _accessTokenExpiration.HasValue && _accessTokenExpiration.Value > DateTime.UtcNow;
        }

        // Token süresinin dolmasına ne kadar kaldığını hesaplar (dakika cinsinden)
        public static double GetTokenRemainingTimeInMinutes()
        {
            if (_accessTokenExpiration.HasValue)
            {
                return (_accessTokenExpiration.Value - DateTime.UtcNow).TotalMinutes;
            }
            return -1; // Token yok veya süresi dolmuş
        }

        public static void ClearSession()
        {
            lock (_lock)
            {
                _cachedAccessToken = null;
                _cachedRefreshToken = null;
                _userId = null;
                _accessTokenExpiration = null;
                _cachedRoles = null;

                try
                {
                    var tokenFilePath = GetTokenFilePath();
                    if (File.Exists(tokenFilePath))
                    {
                        File.Delete(tokenFilePath);
                    }

                    var agentFilePath = GetAgentFilePath();
                    if (File.Exists(agentFilePath))
                    {
                        File.Delete(agentFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Session temizleme hatası: {ex.Message}");
                }
            }
        }

        private static string GetTokenFilePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "data", TOKEN_FILE);
        }

        private static string GetAgentFilePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "data", AGENT_FILE);
        }
    }
}
