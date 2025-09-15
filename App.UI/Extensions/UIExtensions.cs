using App.UI.Application.Services;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Http;
using App.UI.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Reflection;

namespace App.UI.Extensions
{
    public static class UIExtensions
    {
        public static IServiceCollection AddServicesUI(this IServiceCollection services, IConfiguration configuration)
        {
            // Infrastructure Layer
            services.AddInfrastructureServices(configuration);

            // Authentication & Authorization
            services.AddAuthenticationServices();
            services.AddAuthorizationServices();

            // Application Layer
            services.AddApplicationServices();

            // Presentation Layer
            services.AddPresentationServices();

            return services;
        }
        #region Infrastructure Services
        private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Session Configuration
            services.AddHttpContextAccessor();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // HTTP Clients Configuration
            services.AddHttpClientsConfiguration(configuration);

            // Infrastructure Services Registration
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IExternalApiService, ExternalApiService>();

            return services;
        }

        private static IServiceCollection AddHttpClientsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var apiBaseUrl = GetApiBaseUrl(configuration);

            // Main API Client (ApiService için)
            services.AddHttpClient<IApiService, ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            // Auth Client
            services.AddHttpClient("AuthClient", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            // Token Client
            services.AddHttpClient("TokenClient", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            // External API Client
            services.AddHttpClient("ExternalApiClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "SefimPlus-API-Client/1.0");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            return services;
        }
        #endregion

        #region Authentication Services
        private static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
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

            return services;
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
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

        private static void AddAuthorizationServices(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

            });
        }
        #endregion

        #region Application Services
        private static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {

            services.AddScoped<IMachineAppService, MachineAppService>();
            services.AddLegacyServices();

            return services;
        }

        private static IServiceCollection AddLegacyServices(this IServiceCollection services)
        {
            try { services.AddScoped<IAuthService, AuthService>(); } catch { }
            try { services.AddScoped<IRoleService, RoleService>(); } catch { }
            try { services.AddScoped<IMemberService, MemberService>(); } catch { }
            try { services.AddScoped<IAccountService, AccountService>(); } catch { }

            return services;
        }
        #endregion

        #region Presentation Services
        private static IServiceCollection AddPresentationServices(this IServiceCollection services)
        {
            // AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            return services;
        }
        #endregion
        private static string GetApiBaseUrl(IConfiguration configuration)
        {
            // Environment variable'dan API URL'ini al
            var apiUrl = Environment.GetEnvironmentVariable("API_BASE_URL");

            if (!string.IsNullOrEmpty(apiUrl))
            {
                return apiUrl;
            }

            // Docker container'da çalışıyor mu kontrol et
            var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            if (isDocker)
            {
                // Docker ortamında container adını kullan
                return "http://app-api:8080/";
            }
            else
            {
                // Local development
                return "http://localhost:5190/";
            }
        }
    }
}
