using StackExchange.Redis;
using System.Text.Json;

namespace App.Shared.Redis
{
    public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
    {
        public async Task<T?> GetAsync<T>(string key)
        {
            var db = redis.GetDatabase();
            var jsonData = await db.StringGetAsync(key);

            if (jsonData.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(jsonData!);
        }

        public async Task RemoveAsync(string key)
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            var db = redis.GetDatabase();
            var jsonData = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, jsonData, expiration);
        }
    }
}
