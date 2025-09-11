using App.Repositories;
using App.Repositories.Extensions;
using App.Repositories.SeedDatas;
using App.Services.Extensions;
using App.Services.Filters;
using App.Shared;
using App.Shared.Redis;
using App.Worker;
using Microsoft.EntityFrameworkCore;
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

            DotNetEnv.Env.Load();

            LoggerConfig.ConfigureLogger("App.Api");

            // Redis Connection String
            var cacheSettingsSection = builder.Configuration.GetSection("CacheSettings");

            // Redis Cache'i ekle
            builder.Services.AddRedisCache(cacheSettingsSection["ConnectionString"]!).AddElastic(builder.Configuration);
            builder.Services.AddScoped<DatabaseSeeder>();
            builder.Services.AddOpenApi();
            builder.Services.AddRepositories(builder.Configuration).AddServices(builder.Configuration);

            builder.Services.AddHostedService<RabbitMQWorker>();

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

            RepositoryExtensions.ApplyMigrationsAsync(app.Services);

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

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

                try
                {
                    // Database'i sil ve yeniden oluştur (sadece development için)
                    if (app.Environment.IsDevelopment())
                    {
                        // Migration'lar varsa uygula, yoksa oluştur
                        if (context.Database.GetPendingMigrations().Any())
                        {
                            await context.Database.MigrateAsync();
                        }
                        else if (!await context.Database.CanConnectAsync())
                        {
                            await context.Database.EnsureCreatedAsync();
                        }
                    }
                    else
                    {
                        await context.Database.MigrateAsync();
                    }
                    Log.Information("Database migration tamamlandı");

                    await seeder.SeedAsync();
                    Log.Information("Database seed tamamlandı");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Database migration/seed hatası");
                    throw;
                }
            }

            await app.RunAsync();
        }
    }
}
