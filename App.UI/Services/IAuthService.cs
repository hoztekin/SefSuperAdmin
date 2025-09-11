using App.Services.Authentications.DTOs;
using App.UI.DTOS;
using App.UI.Helper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text.Json;
using TokenDto = App.Services.Authentications.DTOs.TokenDto;

namespace App.UI.Services
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

        public AuthService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, ILogger<AuthService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClientFactory.CreateClient("AuthClient");
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
                var (userId, roles) = JwtTokenParser.ParseToken(tokenDto.AccessToken);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("SignInAsync: User ID token'dan çıkarılamadı");
                    return false;
                }

                // Session'ı kaydet
                SessionManager.SaveSession(tokenDto.AccessToken, userId, tokenDto.AccessTokenExpiration, roles, tokenDto.RefreshToken);

                // Claims oluştur
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userId),
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
                var session = SessionManager.GetSession();
                if (session != null && !string.IsNullOrEmpty(session.RefreshToken))
                {
                    try
                    {
                        // API'ye refresh token'ı iptal etmesini söyle
                        var refreshTokenDto = new RefreshTokenDto
                        {
                            Token = session.RefreshToken
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

                // Session'ı temizle
                SessionManager.ClearSession();

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
                var session = SessionManager.GetSession();
                if (session == null || string.IsNullOrEmpty(session.RefreshToken))
                {
                    _logger.LogWarning("RefreshTokenAsync: Session veya refresh token bulunamadı");
                    return false;
                }

                var refreshTokenDto = new RefreshTokenDto
                {
                    Token = session.RefreshToken
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
                    SessionManager.ClearSession();
                    await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var serviceTokenResponse = JsonSerializer.Deserialize<ServiceResult<TokenDto>>(responseString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (serviceTokenResponse?.Data == null)
                {
                    _logger.LogWarning("Token yenileme response'u geçersiz");
                    SessionManager.ClearSession();
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
                SessionManager.ClearSession();
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return false;
            }
        }

        public bool IsAuthenticated()
        {
            var session = SessionManager.GetSession();
            return session != null && SessionManager.IsTokenValid();
        }
    }
}
