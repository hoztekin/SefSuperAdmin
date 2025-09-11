using App.UI.Helper;
using App.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace App.UI.MiddleWare
{
    public class TokenRefreshMiddleware(RequestDelegate next)
    {


        public async Task InvokeAsync(HttpContext context, IAuthService authService, ILogger<TokenRefreshMiddleware> logger)
        {
            // Sadece authenticated kullanıcılar için kontrol yap
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var session = SessionManager.GetSession();

                // Session varsa ve AccessToken süresi kontrol et
                if (session != null && !string.IsNullOrEmpty(session.AccessToken))
                {
                    // Token'ın süresi kontrol et
                    double remainingMinutes = SessionManager.GetTokenRemainingTimeInMinutes();

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
