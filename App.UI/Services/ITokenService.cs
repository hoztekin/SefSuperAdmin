using App.UI.DTOS;
using App.UI.Helper;
using System.Text.Json;

namespace App.UI.Services
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
        Task<bool> RefreshTokenAsync();
        Task<bool> LoginAsync(string userName, string password);
        void Logout();
        bool IsAuthenticated();
    }
    public class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<TokenService> _logger;
        private readonly ISessionService _sessionService;

        public TokenService(IHttpClientFactory httpClientFactory, ISessionService sessionService, ILogger<TokenService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("TokenClient");
            _logger = logger;
            _sessionService = sessionService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<string> GetTokenAsync()
        {
            var userSession = _sessionService.GetUserSession();

            // Oturum yoksa veya token süresi dolmuşsa, yenilemeyi dene
            if (userSession == null || !_sessionService.IsAuthenticated())
            {
                _logger.LogInformation("Token geçersiz, yenileme deneniyor...");
                var refreshSuccess = await RefreshTokenAsync();
                if (!refreshSuccess)
                {
                    _logger.LogWarning("Token yenilenemedi");
                    return null;
                }
                userSession = _sessionService.GetUserSession();
            }

            return userSession?.AccessToken;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            var userSession = _sessionService.GetUserSession();
            if (userSession == null || string.IsNullOrEmpty(userSession.RefreshToken))
            {
                _logger.LogWarning("RefreshToken bulunamadı");
                return false;
            }

            try
            {
                var refreshTokenDto = new { Token = userSession.RefreshToken };
                var content = new StringContent(
                    JsonSerializer.Serialize(refreshTokenDto, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("api/v1/auth/CreateTokenByRefreshToken", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token yenileme başarısız: {StatusCode}", response.StatusCode);

                    _sessionService.ClearUserSession();
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

                var currentUserInfo = userSession.UserInfo ?? new UserInfoDto { Id = userId };

                _sessionService.SaveUserSession(
                    accessToken: tokenResponse.Data.AccessToken,
                    refreshToken: tokenResponse.Data.RefreshToken,
                    expiresAt: tokenResponse.Data.AccessTokenExpiration,
                    userInfo: currentUserInfo,
                    roles: roles ?? new List<string>(),
                    permissions: userSession.Permissions ?? new List<string>()
                );

                _logger.LogInformation("Token başarıyla yenilendi");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token yenileme hatası");
                _sessionService.ClearUserSession();
                return false;
            }
        }

        public async Task<bool> LoginAsync(string userName, string password)
        {
            try
            {
                var loginDto = new { userName = userName, Password = password };
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

                var userInfo = new UserInfoDto
                {
                    Id = userId,
                    UserName = userName
                };

                _sessionService.SaveUserSession(
                    accessToken: tokenResponse.Data.AccessToken,
                    refreshToken: tokenResponse.Data.RefreshToken,
                    expiresAt: tokenResponse.Data.AccessTokenExpiration,
                    userInfo: userInfo,
                    roles: roles ?? new List<string>(),
                    permissions: new List<string>()
                );

                _logger.LogInformation("Login başarılı: {UserId} - {UserName}", userId, userName);
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
            _sessionService.ClearUserSession();
            _logger.LogInformation("Logout işlemi tamamlandı");
        }

        public bool IsAuthenticated()
        {
            return _sessionService.IsAuthenticated();
        }
    }
}
