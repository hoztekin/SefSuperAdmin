using App.Shared;
using App.UI.Extensions;
using App.UI.MiddleWare;
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

            // Authorization
            app.UseAuthorization();

            // Custom Middleware
            app.UseMiddleware<TokenRefreshMiddleware>();


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
