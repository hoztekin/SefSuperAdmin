using App.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace App.UI.MiddleWare
{
    public class TokenRefreshMiddleware(RequestDelegate next)
    {


        public async Task InvokeAsync(HttpContext context, IAuthService authService, ISessionService sessionService, ILogger<TokenRefreshMiddleware> logger)
        {
            // Sadece authenticated kullanıcılar için kontrol yap
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userSession = sessionService.GetUserSession();

                // Session varsa ve AccessToken süresi kontrol et
                if (userSession != null && !string.IsNullOrEmpty(userSession.AccessToken))
                {
                    // Token'ın süresi kontrol et
                    double remainingMinutes = (userSession.ExpiresAt - DateTime.UtcNow).TotalMinutes;

                    if (remainingMinutes <= 0)
                    {
                        // Token süresi dolmuş, refresh token ile yenilemeyi dene
                        logger.LogInformation("Token süresi dolmuş, yenileme deneniyor...");

                        bool refreshed = await authService.RefreshTokenAsync();
                        if (!refreshed)
                        {
                            // Refresh token da geçersiz, oturumu sonlandır
                            logger.LogWarning("Token yenilenemedi, oturum sonlandırılıyor...");
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            context.Response.Redirect("/Authentication/Login");
                            return;
                        }
                    }
                    else if (remainingMinutes > 0 && remainingMinutes < 5)
                    {
                        // Token'ın süresi 5 dakikadan az kaldıysa yenile
                        logger.LogInformation("Token süresi az kaldı ({minutes} dakika), yenileme deneniyor...", remainingMinutes);

                        await authService.RefreshTokenAsync();
                    }
                }
                else
                {
                    // Session yoksa veya token yoksa logout yap
                    logger.LogWarning("User session bulunamadı, oturum sonlandırılıyor...");
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Authentication/Login");
                    return;
                }
            }

            await next(context);

            // Response 401 ise token yenilemeyi dene
            if (context.Response.StatusCode == 401 && context.User.Identity?.IsAuthenticated == true)
            {
                logger.LogInformation("401 alındı, token yenileme deneniyor...");

                bool refreshed = await authService.RefreshTokenAsync();
                if (refreshed)
                {
                    // Token yenilendi, orijinal request'i tekrar dene
                    context.Response.Redirect(context.Request.Path);
                }
                else
                {
                    // Token yenilenemedi, login sayfasına yönlendir
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Authentication/Login");
                }
            }
        }
    }
}
