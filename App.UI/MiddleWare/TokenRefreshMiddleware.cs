using App.UI.Helper;
using App.UI.Services;

namespace App.UI.MiddleWare
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenRefreshMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            // İsteği işlemeye devam etmeden önce token kontrolü yap
            var session = SessionManager.GetSession();

            // Session varsa ve AccessToken süresi kontrol et
            if (session != null && !string.IsNullOrEmpty(session.AccessToken))
            {
                // Token'ın süresi 5 dakikadan az kaldıysa yenile
                double remainingMinutes = SessionManager.GetTokenRemainingTimeInMinutes();
                if (remainingMinutes > 0 && remainingMinutes < 5)
                {
                    await authService.RefreshTokenAsync();
                }
            }

            await _next(context);

            if (context.Response.StatusCode == 401)
            {
                bool refreshed = await authService.RefreshTokenAsync();

                if (refreshed)
                {
                    context.Response.Redirect(context.Request.Path);
                }
                else
                {
                    context.Response.Redirect("/Authentication/Login");
                }
            }
        }
    }
}
