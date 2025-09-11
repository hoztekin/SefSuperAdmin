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
            builder.Host.UseSerilog();
            LoggerConfig.ConfigureLogger("App.UI");
            builder.Services.AddControllersWithViews();
            builder.Services.AddServicesUI();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options =>
            {
                CookieBuilder cookie = new CookieBuilder();
                options.LoginPath = new PathString("/Authentication/Login");
                options.LogoutPath = new PathString("/Authentication/Logout");
                options.AccessDeniedPath = new PathString("/Authentication/AccessDenied");
                options.Cookie.HttpOnly = false;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;
            });



            var app = builder.Build();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
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
