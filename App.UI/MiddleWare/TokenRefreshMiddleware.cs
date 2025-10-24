using App.UI.Application.Services;
using App.UI.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace App.UI.MiddleWare
{
    public class TokenRefreshMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, IAuthService authService, ISessionService sessionService, ILogger<TokenRefreshMiddleware> logger)
        {
            try
            {
                // Sadece authenticated kullanıcılar için kontrol et
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var userSession = sessionService.GetUserSession();

                    // Session var mı kontrol et
                    if (userSession != null && !string.IsNullOrEmpty(userSession.AccessToken))
                    {
                        // Token'ın kalan süresini hesapla
                        double remainingMinutes = (userSession.ExpiresAt - DateTime.UtcNow).TotalMinutes;

                        // Token süresi dolmuşsa yenile
                        if (remainingMinutes <= 0)
                        {
                            logger.LogInformation("Token süresi dolmuş, RefreshToken ile yenileniyor...");
                            bool refreshed = await authService.RefreshTokenAsync();

                            if (!refreshed)
                            {
                                logger.LogWarning("RefreshToken başarısız, logout yapılıyor");
                                await LogoutUserAsync(context);
                                return;
                            }
                        }
                        // Token'ın süresi 5 dakikadan az kaldıysa yenile
                        else if (remainingMinutes > 0 && remainingMinutes < 5)
                        {
                            logger.LogInformation("Token süresi az kaldı ({Minutes} dakika), yenileniyor", remainingMinutes);
                            await authService.RefreshTokenAsync();
                        }
                    }
                    else
                    {
                        // Session yoksa RefreshToken ile yeni token al
                        logger.LogInformation("Session bulunamadı, RefreshToken ile yenileniyor");
                        bool refreshed = await authService.RefreshTokenAsync();

                        if (!refreshed)
                        {
                            logger.LogWarning("RefreshToken başarısız, logout yapılıyor");
                            await LogoutUserAsync(context);
                            return;
                        }
                    }
                }

                // Sonraki middleware'e geç
                await next(context);

                // Response 401 ise token yenilemeyi dene
                if (context.Response.StatusCode == 401 && context.User.Identity?.IsAuthenticated == true)
                {
                    logger.LogInformation("401 hatası alındı, RefreshToken ile yenileniyor");
                    bool refreshed = await authService.RefreshTokenAsync();

                    if (refreshed)
                    {
                        // Token yenilendi
                        logger.LogInformation("Token başarıyla yenilendi");
                    }
                    else
                    {
                        // Token yenilenemedi
                        logger.LogWarning("RefreshToken başarısız, logout yapılıyor");
                        await LogoutUserAsync(context);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TokenRefreshMiddleware'de hata oluştu");
                await next(context);
            }
        }

        private async Task LogoutUserAsync(HttpContext context)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/Authentication/Login");
        }
    }
}
