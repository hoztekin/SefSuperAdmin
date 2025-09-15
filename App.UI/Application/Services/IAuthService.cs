using App.Services.Authentications.DTOs;
using App.UI.Application.DTOS;
using App.UI.Helper;
using App.UI.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text.Json;
using TokenDto = App.Services.Authentications.DTOs.TokenDto;

namespace App.UI.Application.Services
{
    public interface IAuthService
    {
        Task<bool> SignInAsync(TokenDtoUI tokenDto);
        Task SignOutAsync();
        Task<bool> RefreshTokenAsync();
        bool IsAuthenticated();
    }

    public class AuthService : IAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;
        private readonly ISessionService _sessionService;

        public AuthService(IHttpContextAccessor httpContextAccessor, ISessionService sessionService, IHttpClientFactory httpClientFactory, ILogger<AuthService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClientFactory.CreateClient("AuthClient");
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task<bool> SignInAsync(TokenDtoUI tokenDto)
        {
            if (tokenDto == null || string.IsNullOrEmpty(tokenDto.AccessToken))
            {
                _logger.LogWarning("SignInAsync: Token bilgisi eksik");
                return false;
            }

            try
            {
                // JWT token'dan kullanıcı bilgilerini çıkar
                var (userId, roles, username) = JwtTokenParser.ParseToken(tokenDto.AccessToken);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("SignInAsync: User ID token'dan çıkarılamadı");
                    return false;
                }

                // Session'ı kaydet
                var userInfo = new UserInfoDto { Id = userId };

                _sessionService.SaveUserSession(
                    accessToken: tokenDto.AccessToken,
                    refreshToken: tokenDto.RefreshToken,
                    expiresAt: tokenDto.AccessTokenExpiration,
                    userInfo: userInfo,
                    roles: roles ?? new List<string>(),
                    permissions: new List<string>()
                );

                // Claims oluştur
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, !string.IsNullOrEmpty(username) ? username : userId),
                    new Claim("AccessToken", tokenDto.AccessToken),
                    new Claim("AccessTokenExpiration", tokenDto.AccessTokenExpiration.ToString("O"))
                };

                // Rolleri ekle
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Identity oluştur
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Cookie olarak oturum aç
                await _httpContextAccessor.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = tokenDto.AccessTokenExpiration,
                        AllowRefresh = true
                    });

                _logger.LogInformation("Kullanıcı başarıyla giriş yaptı: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignInAsync sırasında hata oluştu");
                return false;
            }
        }

        public async Task SignOutAsync()
        {
            try
            {
                var userSession = _sessionService.GetUserSession();

                if (userSession != null && !string.IsNullOrEmpty(userSession.RefreshToken))
                {
                    try
                    {
                        // API'ye refresh token'ı iptal etmesini söyle
                        var refreshTokenDto = new RefreshTokenDto
                        {
                            Token = userSession.RefreshToken
                        };

                        var json = JsonSerializer.Serialize(refreshTokenDto);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        await _httpClient.PostAsync("api/v1/Auth/RevokeRefreshToken", content);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Refresh token iptal edilemedi");
                    }
                }

                _sessionService.ClearUserSession();

                // Cookie'yi temizle
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("Kullanıcı çıkış yaptı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignOutAsync sırasında hata oluştu");
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                // ✅ YENİ: ISessionService kullan
                var userSession = _sessionService.GetUserSession();

                if (userSession == null || string.IsNullOrEmpty(userSession.RefreshToken))
                {
                    _logger.LogWarning("RefreshTokenAsync: Session veya refresh token bulunamadı");
                    return false;
                }

                var refreshTokenDto = new RefreshTokenDto
                {
                    Token = userSession.RefreshToken
                };

                var json = JsonSerializer.Serialize(refreshTokenDto, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/v1/Auth/CreateTokenByRefreshToken", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token yenileme başarısız: {StatusCode}", response.StatusCode);

                    // Eğer refresh token da geçersizse session'ı temizle
                    _sessionService.ClearUserSession();
                    await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var serviceTokenResponse = JsonSerializer.Deserialize<ServiceResult<TokenDto>>(responseString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (serviceTokenResponse?.Data == null)
                {
                    _logger.LogWarning("Token yenileme response'u geçersiz");
                    _sessionService.ClearUserSession();
                    await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return false;
                }

                // Services TokenDto'yu UI TokenDtoUI'ye çevir
                var uiTokenDto = new TokenDtoUI
                {
                    AccessToken = serviceTokenResponse.Data.AccessToken,
                    AccessTokenExpiration = serviceTokenResponse.Data.AccessTokenExpiration,
                    RefreshToken = serviceTokenResponse.Data.RefreshToken,
                    RefreshTokenExpiration = serviceTokenResponse.Data.RefreshTokenExpiration
                };

                // Yeni token ile tekrar sign-in yap
                var result = await SignInAsync(uiTokenDto);
                if (result)
                {
                    _logger.LogInformation("Token başarıyla yenilendi");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefreshTokenAsync sırasında hata oluştu");
                _sessionService.ClearUserSession();
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return false;
            }
        }

        public bool IsAuthenticated()
        {
            return _sessionService.IsAuthenticated();
        }
    }
}
