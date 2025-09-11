using App.UI.DTOS;
using App.UI.Helper;
using System.Text.Json;

namespace App.UI.Services
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
        Task<bool> RefreshTokenAsync();
        Task<bool> LoginAsync(string email, string password);
        void Logout();
        bool IsAuthenticated();
    }
    public class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public TokenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<string> GetTokenAsync()
        {
            var session = SessionManager.GetSession();

            // Oturum yoksa veya token süresi dolmuşsa, yenilemeyi dene
            if (session == null || !SessionManager.IsTokenValid())
            {
                var refreshSuccess = await RefreshTokenAsync();
                if (!refreshSuccess)
                {
                    return null;
                }
                session = SessionManager.GetSession(); // Yenileme sonrası güncel oturumu al
            }

            return session?.AccessToken;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            var session = SessionManager.GetSession();
            if (session == null || string.IsNullOrEmpty(session.RefreshToken))
            {
                return false;
            }

            try
            {
                var refreshTokenDto = new { Token = session.RefreshToken };
                var content = new StringContent(
                    JsonSerializer.Serialize(refreshTokenDto, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("api/v1/auth/CreateTokenByRefreshToken", content);

                if (!response.IsSuccessStatusCode)
                {
                    SessionManager.ClearSession();
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseString, _jsonOptions);

                if (tokenResponse?.Data == null)
                {
                    return false;
                }

                // Token'dan userId ve roller bilgisini al
                var (userId, roles) = JwtTokenParser.ParseToken(tokenResponse.Data.AccessToken);

                // Yeni oturumu kaydet
                SessionManager.SaveSession(tokenResponse.Data.AccessToken, userId, tokenResponse.Data.AccessTokenExpiration, roles, tokenResponse.Data.RefreshToken);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token yenileme hatası: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var loginDto = new { Email = email, Password = password };
                var content = new StringContent(
                    JsonSerializer.Serialize(loginDto, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("api/v1/Auth/Login", content);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseString, _jsonOptions);

                if (tokenResponse?.Data == null)
                {
                    return false;
                }

                // Token'dan userId ve roller bilgisini al
                var (userId, roles) = JwtTokenParser.ParseToken(tokenResponse.Data.AccessToken);

                // Oturumu kaydet
                SessionManager.SaveSession(tokenResponse.Data.AccessToken, userId, tokenResponse.Data.AccessTokenExpiration, roles, tokenResponse.Data.RefreshToken);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Giriş hatası: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        public void Logout()
        {
            SessionManager.ClearSession();
        }

        public bool IsAuthenticated()
        {
            return SessionManager.GetSession() != null && SessionManager.IsTokenValid();
        }
    }
}
