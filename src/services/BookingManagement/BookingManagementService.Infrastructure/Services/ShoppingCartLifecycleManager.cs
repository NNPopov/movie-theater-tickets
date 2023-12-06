using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure.Services;

public class ShoppingCartLifecycleManager : IShoppingCartLifecycleManager
{
    private readonly IConnectionMultiplexer _redis;

    private const string KeyPrefixTimeToLive = "shopping_cart_ttl";
    
    private readonly ShoppingCartConfiguration _shoppingCartConfiguration;
    
    public ShoppingCartLifecycleManager(IConnectionMultiplexer redis, IOptionsSnapshot<ShoppingCartConfiguration> config)
    {
        _redis = redis;
        _shoppingCartConfiguration = config.Value;
    }

    public async Task<SeatShoppingCart> GetAsync(Guid shoppingCartId)
    {
        var db = _redis.GetDatabase();
        var timeToLiveKey = GetKey(shoppingCartId);   

        string jsonValue = await db.StringGetAsync(timeToLiveKey);

        if (string.IsNullOrEmpty(jsonValue))
            return default;

        return JsonConvert.DeserializeObject<SeatShoppingCart>(jsonValue);
    }

    public async Task DeleteAsync(Guid shoppingCartId)
    {
        var db = _redis.GetDatabase();
        var timeToLiveKey = GetKey(shoppingCartId);   
        await db.KeyDeleteAsync(timeToLiveKey);
    }

    private static string GetKey(Guid shoppingCartId)
    {
        return $"{KeyPrefixTimeToLive}:{shoppingCartId.ToString()}"; 
    }

    public async Task SetAsync(Guid shoppingCartId)
    {
        var expiry =TimeSpan.FromSeconds(_shoppingCartConfiguration.ShoppingCartTimeToLiveSec);
        
        var db = _redis.GetDatabase();
        var timeToLiveKey = GetKey(shoppingCartId);   
        await db.StringSetAsync(timeToLiveKey, timeToLiveKey, expiry);
    }
    
}