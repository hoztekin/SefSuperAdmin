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
        private readonly ILogger<TokenService> _logger;

        public TokenService(IHttpClientFactory httpClientFactory, ILogger<TokenService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("TokenClient"); 
            _logger = logger;
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
                _logger.LogInformation("Token geçersiz, yenileme deneniyor...");
                var refreshSuccess = await RefreshTokenAsync();
                if (!refreshSuccess)
                {
                    _logger.LogWarning("Token yenilenemedi");
                    return null;
                }
                session = SessionManager.GetSession();
            }

            return session?.AccessToken;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            var session = SessionManager.GetSession();
            if (session == null || string.IsNullOrEmpty(session.RefreshToken))
            {
                _logger.LogWarning("RefreshToken bulunamadı");
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
                    _logger.LogWarning("Token yenileme başarısız: {StatusCode}", response.StatusCode);
                    SessionManager.ClearSession();
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseString, _jsonOptions);

                if (tokenResponse?.Data == null)
                {
                    _logger.LogWarning("Token response geçersiz");
                    return false;
                }

                // Token'dan userId ve roller bilgisini al
                var (userId, roles) = JwtTokenParser.ParseToken(tokenResponse.Data.AccessToken);

                // Yeni oturumu kaydet
                SessionManager.SaveSession(
                    tokenResponse.Data.AccessToken,
                    userId,
                    tokenResponse.Data.AccessTokenExpiration,
                    roles,
                    tokenResponse.Data.RefreshToken);

                _logger.LogInformation("Token başarıyla yenilendi");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token yenileme hatası");
                SessionManager.ClearSession();
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
                    _logger.LogWarning("Login başarısız: {StatusCode}", response.StatusCode);
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseString, _jsonOptions);

                if (tokenResponse?.Data == null)
                {
                    _logger.LogWarning("Login response geçersiz");
                    return false;
                }

                // Token'dan userId ve roller bilgisini al
                var (userId, roles) = JwtTokenParser.ParseToken(tokenResponse.Data.AccessToken);

                // Oturumu kaydet
                SessionManager.SaveSession(
                    tokenResponse.Data.AccessToken,
                    userId,
                    tokenResponse.Data.AccessTokenExpiration,
                    roles,
                    tokenResponse.Data.RefreshToken);

                _logger.LogInformation("Login başarılı: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login işlemi sırasında hata");
                return false;
            }
        }

        public void Logout()
        {
            SessionManager.ClearSession();
            _logger.LogInformation("Logout işlemi tamamlandı");
        }

        public bool IsAuthenticated()
        {
            return SessionManager.GetSession() != null && SessionManager.IsTokenValid();
        }
    }
}
