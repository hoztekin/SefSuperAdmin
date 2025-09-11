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
        Task<bool> SignInAsync(TokenDto tokenDto);
        Task SignOutAsync();
        Task<bool> RefreshTokenAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public AuthService(IApiService apiService, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _apiService = apiService;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClientFactory.CreateClient("AuthClient");
        }

        public async Task<bool> SignInAsync(TokenDto tokenDto)
        {
            if (tokenDto == null) return false;

            // JWT token'dan kullanıcı bilgilerini çıkar
            var (userId, roles) = JwtTokenParser.ParseToken(tokenDto.AccessToken);

            // Session'ı kaydet
            SessionManager.SaveSession(tokenDto.AccessToken, userId, tokenDto.AccessTokenExpiration, roles, tokenDto.RefreshToken);

            // Claims oluştur
            var claims = new List<Claim>
            {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId)
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
                    ExpiresUtc = tokenDto.AccessTokenExpiration
                });

            return true;
        }

        public async Task SignOutAsync()
        {
            var session = SessionManager.GetSession();
            if (session != null && !string.IsNullOrEmpty(session.RefreshToken))
            {
                try
                {
                    var refreshTokenDto = new RefreshTokenDto
                    {
                        Token = session.RefreshToken
                    };
                    await _apiService.PostAsync<RefreshTokenDto>("api/v1/Auth/RevokeRefreshToken", refreshTokenDto);
                }
                catch
                {

                }
            }
            SessionManager.ClearSession();
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<bool> RefreshTokenAsync()
        {
            var session = SessionManager.GetSession();
            if (session == null || string.IsNullOrEmpty(session.RefreshToken))
                return false;

            try
            {
                var refreshTokenDto = new RefreshTokenDto
                {
                    Token = session.RefreshToken
                };

                var content = new StringContent(
               JsonSerializer.Serialize(refreshTokenDto, new JsonSerializerOptions
               {
                   PropertyNameCaseInsensitive = true
               }),
               System.Text.Encoding.UTF8,
               "application/json");

                var response = await _httpClient.PostAsync("api/v1/Auth/CreateTokenByRefreshToken", content);

                if (!response.IsSuccessStatusCode)
                {
                    SessionManager.ClearSession();
                    await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<ServiceResult<TokenDto>>(responseString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokenResponse?.Data == null)
                {
                    SessionManager.ClearSession();
                    await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return false;
                }

                return await SignInAsync(tokenResponse.Data);
            }
            catch
            {
                SessionManager.ClearSession();
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return false;
            }
        }
    }
}
