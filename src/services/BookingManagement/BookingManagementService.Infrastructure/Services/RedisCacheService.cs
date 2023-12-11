using CinemaTicketBooking.Application.Abstractions.Services;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<T?> TryGet<T>(string cacheKey)
    {
        var db = _redis.GetDatabase();

        var jsonValue = await db.StringGetAsync(cacheKey);

        if (string.IsNullOrEmpty(jsonValue.ToString()))
            return default;

        var value = JsonConvert.DeserializeObject<T>(jsonValue.ToString());

        return value;
    }

    public async Task<T> Set<T>(string cacheKey, T value, TimeSpan? expiry)
    {
        var db = _redis.GetDatabase();

        string jsonValue = JsonConvert.SerializeObject(value);

        await db.StringSetAsync(cacheKey, jsonValue);

        return value;
    }

    public async Task Remove(string cacheKey)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(cacheKey);
    }
}