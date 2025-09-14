using App.UI.Services;
using System.Reflection;

namespace App.UI.Extensions
{
    public static class UIExtensions
    {
        public static IServiceCollection AddServicesUI(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2); // 2 saat session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
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
