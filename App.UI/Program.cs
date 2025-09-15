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

            // Configure Logging
            ConfigureLogging(builder);

            // Configure Services
            ConfigureServices(builder.Services, builder.Configuration);

            // Build Application
            var app = builder.Build();

            // Configure Pipeline
            ConfigurePipeline(app);

            app.Run();
        }
        #region Service Configuration
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Core MVC Services
            services.AddControllersWithViews();

            // UI Layer Services (Infrastructure + Application + Presentation)
            services.AddServicesUI(configuration);

            // Authentication & Authorization
            ConfigureAuthentication(services);
            ConfigureAuthorization(services);
        }

        private static void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                // Cookie Settings
                options.LoginPath = new PathString("/Authentication/Login");
                options.LogoutPath = new PathString("/Authentication/Logout");
                options.AccessDeniedPath = new PathString("/Authentication/AccessDenied");

                options.Cookie.Name = "AppAuthCookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                // Expiration Settings
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;

                // Token Validation Event
                options.Events.OnValidatePrincipal = ValidateUserToken;
            });
        }

        private static void ConfigureAuthorization(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // TODO: Özel authorization policy'leri buraya eklenebilir
                // options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                // options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
            });
        }

        private static async Task ValidateUserToken(CookieValidatePrincipalContext context)
        {
            try
            {
                // Token geçerliliğini kontrol et
                var sessionService = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
                var userSession = sessionService.GetUserSession();

                if (userSession == null || userSession.IsExpired)
                {
                    // Token geçersizse oturumu sonlandır
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // Log the event
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("User session expired, signing out user");
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda oturumu sonlandır
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error validating user token");

                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
        #endregion

        #region Pipeline Configuration
        private static void ConfigurePipeline(WebApplication app)
        {
            // Exception Handling
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            // Static Files & Security
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Routing
            app.UseRouting();

            // Authentication & Session
            app.UseAuthentication();
            app.UseSession();

            // Custom Middleware
            app.UseMiddleware<TokenRefreshMiddleware>();

            // Authorization
            app.UseAuthorization();

            // Static Assets & Routes
            app.MapStaticAssets();
            ConfigureRoutes(app);
        }

        private static void ConfigureRoutes(WebApplication app)
        {
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Authentication}/{action=Login}/{id?}")
                .WithStaticAssets();

        }
        #endregion

        #region Logging Configuration
        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog();
            LoggerConfig.ConfigureLogger("App.UI");
        }
        #endregion
    }
}
