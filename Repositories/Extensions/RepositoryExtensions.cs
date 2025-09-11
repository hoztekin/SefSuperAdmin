using App.Repositories.Machines;
using App.Repositories.UserRefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace App.Repositories.Extensions
{
    public static class RepositoryExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                var connectionStrings = configuration.GetSection(ConnectionStringOption.Key).Get<ConnectionStringOption>();

                options.UseSqlServer(connectionStrings!.SqlServer, sqlServerOptionsAction =>
                {
                    sqlServerOptionsAction.MigrationsAssembly(typeof(RepositoryAssembly).Assembly.FullName);

                    sqlServerOptionsAction.EnableRetryOnFailure(
                                   maxRetryCount: 5,
                                   maxRetryDelay: TimeSpan.FromSeconds(30),
                                   errorNumbersToAdd: null);
                });

                options.AddInterceptors(new AuditDbContextInterceptor());
            });

            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IMachineRepository, MachineRepository>();
            services.AddScoped<IUserRefreshTokenRepository, UserRefreshTokenRepository>();

            return services;
        }

        #region Docker ile çalışırken otomatik db oluşturması için 

        public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                Log.Information("Veritabanı migrationları uygulanıyor...");
                await dbContext.Database.MigrateAsync();
                Log.Information("Veritabanı migrationları başarıyla uygulandı.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Veritabanı migrationları uygulanırken bir hata oluştu.");
                throw;
            }
        }
        #endregion
    }
}
