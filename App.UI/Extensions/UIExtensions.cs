using App.UI.Services;
using System.Reflection;

namespace App.UI.Extensions
{
    public static class UIExtensions
    {
        public static IServiceCollection AddServicesUI(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddHttpClient<IApiService, ApiService>(client =>
            {
                client.BaseAddress = new Uri("http://app-api:8080/");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            services.AddHttpClient<ITokenService, TokenService>(client =>
            {
                client.BaseAddress = new Uri("http://host.docker.internal:5190/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            services.AddHttpClient("AuthClient", client =>
            {
                client.BaseAddress = new Uri("http://app-api:8080/");
            });

            //services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IMemberService, MemberService>();
            services.AddScoped<IAccountService, AccountService>();


            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
