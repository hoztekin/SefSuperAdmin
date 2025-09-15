using App.UI.Services;
using System.Configuration;
using System.Reflection;

namespace App.UI.Extensions
{
    public static class UIExtensions
    {
        public static IServiceCollection AddServicesUI(this IServiceCollection services)
        {

            // Infrastructure Services
            services.AddInfrastructureServices(configuration);

            // Application Services  
            services.AddApplicationServices();

            // Presentation Services
            services.AddPresentationServices();

            return services;





            services.AddHttpContextAccessor();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2); // 2 saat session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // API Base URL'ini configuration'dan al
            var apiBaseUrl = GetApiBaseUrl();

            // Ana API Client (ApiService için)
            services.AddHttpClient<IApiService, ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            // Auth Client (AuthService için)
            services.AddHttpClient("AuthClient", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            // Token Client (TokenService için - aynı API'yi kullanıyor)
            services.AddHttpClient("TokenClient", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            // External API için ayrı HttpClient
            services.AddHttpClient("ExternalApiClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                // Default headers
                client.DefaultRequestHeaders.Add("User-Agent", "SefimPlus-API-Client/1.0");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });



            // Service registrations
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IMemberService, MemberService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IExternalApiService, ExternalApiService>();

            // AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            return services;
        }

        private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // HTTP Clients
            services.AddHttpClient<IApiHttpClient, ApiHttpClient>(client =>
            {
                client.BaseAddress = new Uri(GetApiBaseUrl(configuration));
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Storage & Session
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddScoped<ISessionManager, SessionManager>();
            services.AddScoped<ITokenStorage, CookieTokenStorage>();

            return services;
        }

        private static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Application Services (Business logic wrappers)
            services.AddScoped<IMachineAppService, MachineAppService>();
            services.AddScoped<IUserAppService, UserAppService>();
            services.AddScoped<IRoleAppService, RoleAppService>();
            services.AddScoped<IAuthAppService, AuthAppService>();
            services.AddScoped<IAccountAppService, AccountAppService>();

            return services;
        }

        private static IServiceCollection AddPresentationServices(this IServiceCollection services)
        {
            // AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // View Services
            services.AddScoped<IViewModelMapper, ViewModelMapper>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<INavigationService, NavigationService>();

            return services;
        }


        private static string GetApiBaseUrl()
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
