using App.Repositories.Extensions;
using App.Repositories.SeedDatas;
using App.Services.Extensions;
using App.Services.Filters;
using App.Shared;
using App.Shared.Redis;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;


namespace App.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // HTTPS'i devre dışı bırak (container için)
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(8080); // Sadece HTTP

                // Timeout ayarları (502 hatalarını önlemek için)
                serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
                serverOptions.Limits.MaxConcurrentConnections = 1000;
                serverOptions.Limits.MaxConcurrentUpgradedConnections = 1000;
                serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
                serverOptions.Limits.MinRequestBodyDataRate = new Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate(
                    bytesPerSecond: 100,
                    gracePeriod: TimeSpan.FromSeconds(10)
                );
            });


            LoggerConfig.ConfigureLogger("App.Api");

            // Redis Connection String
            var cacheSettingsSection = builder.Configuration.GetSection("CacheSettings");

            // Redis Cache'i ekle
            builder.Services.AddRedisCache(cacheSettingsSection["ConnectionString"]!).AddElastic(builder.Configuration);
            builder.Services.AddScoped<DatabaseSeeder>();
            builder.Services.AddOpenApi();
            builder.Services.AddRepositories(builder.Configuration).AddServices(builder.Configuration);

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<FluentValidationFilter>();
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            var app = builder.Build();

            // Migration'ı opsiyonel yap (startup süresini azaltmak için)
            var skipMigration = builder.Configuration.GetValue<bool>("SKIP_MIGRATION", false);
            if (!skipMigration)
            {
                try
                {
                    await RepositoryExtensions.ApplyMigrationsAsync(app.Services);
                    Log.Information("Database migration tamamlandı");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Database migration hatası - uygulama yine de başlatılıyor");
                }
            }
            else
            {
                Log.Information("Migration atlandı (SKIP_MIGRATION=true)");
            }

            app.UseExceptionHandler(x => { });
            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }
            //app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseAuthorization();
            app.MapControllers();

            app.MapGet("/health", () =>
            {
                return Results.Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    service = "SefimPlus.Api"
                });
            });


            // Database seeding (opsiyonel)
            if (!skipMigration)
            {
                using (var scope = app.Services.CreateScope())
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

                    try
                    {
                        await seeder.SeedAsync();
                        Log.Information("Database seed tamamlandı");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Database seed hatası - uygulama yine de başlatılıyor");
                    }
                }
            }

            await app.RunAsync();
        }
    }
}
