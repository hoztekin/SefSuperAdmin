using App.Shared;
using App.UI.Extensions;
using App.UI.Infrastructure.Storage;
using App.UI.MiddleWare;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;

namespace App.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();
            LoggerConfig.ConfigureLogger("App.UI");
            builder.Services.AddControllersWithViews();
            builder.Services.AddServicesUI();
            // Authentication konfigürasyonu
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = new PathString("/Authentication/Login");
                options.LogoutPath = new PathString("/Authentication/Logout");
                options.AccessDeniedPath = new PathString("/Authentication/AccessDenied");
                options.Cookie.Name = "AppAuthCookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;

                options.Events.OnValidatePrincipal = async context =>
                {
                    // Token geçerliliğini kontrol et
                    var sessionService = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
                    var userSession = sessionService.GetUserSession();
                    if (userSession == null || userSession.IsExpired)
                    {
                        // Token geçersizse oturumu sonlandır
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });

            // Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            var app = builder.Build();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseSession();
            app.UseMiddleware<TokenRefreshMiddleware>();
            app.UseAuthorization();
            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Authentication}/{action=Login}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
