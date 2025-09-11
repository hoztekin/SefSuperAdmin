using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace App.Shared.Redis
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services, string redisConnectionString)
        {
            // Redis bağlantısını yapılandırıyoruz
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.ConnectRetry = 5; // Yeniden deneme sayısı
                options.ConnectTimeout = 10000; // Zaman aşımı süresi (ms)
                options.SyncTimeout = 10000; // Eş zamanlı işlemler için zaman aşımı
                options.AbortOnConnectFail = false; // İlk bağlantıda hata kesilmesin
                var redis = ConnectionMultiplexer.Connect(options);

                // Redis bağlantı hatalarını loglama
                redis.ConnectionFailed += (sender, args) =>
                {
                    Console.WriteLine($"Redis Connection Failed: {args.Exception.Message}");
                };

                redis.ErrorMessage += (sender, args) =>
                {
                    Console.WriteLine($"Redis Error: {args.Message}");
                };

                return redis;
            });

            // IDistributedCache için Redis yapılandırması
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "SefimPlus_";
            });

            // Redis Cache Servisini ekle
            services.AddSingleton<ICacheService, RedisCacheService>();

            return services;
        }
    }
}
