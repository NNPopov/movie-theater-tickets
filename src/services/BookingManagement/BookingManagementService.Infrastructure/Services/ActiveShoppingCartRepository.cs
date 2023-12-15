using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure.Services;

public class ActiveShoppingCartRepository : IActiveShoppingCartRepository
{
    private readonly IConnectionMultiplexer _redis;
    
    private const string ShoppingCartKeyPrefix = "cart";
    
    private const string ClientKeyPrefix = "client_session";
    
    private readonly IDomainEventTracker _domainEventTracker;

    public ActiveShoppingCartRepository(IConnectionMultiplexer redis,
        IDomainEventTracker domainEventTracker)
    {
        _redis = redis;
        _domainEventTracker = domainEventTracker;
    }

    public async Task<ShoppingCart> SaveAsync(ShoppingCart shoppingCart)
    {
        var db = _redis.GetDatabase();

        var kartKey = GetShoppingCartKey(shoppingCart.Id.ToString());

        string jsonValue = JsonConvert.SerializeObject(shoppingCart);

        await db.StringSetAsync(kartKey, jsonValue, new TimeSpan(0, 0, 3600));

        await _domainEventTracker.PublishDomainEvents(shoppingCart);

        return shoppingCart!;
    }

    public async Task DeleteAsync(ShoppingCart shoppingCart)
    {
        var db = _redis.GetDatabase();
        
        var kartKey = GetShoppingCartKey(shoppingCart.Id.ToString());

        await db.KeyDeleteAsync(kartKey);
        

        await _domainEventTracker.PublishDomainEvents(shoppingCart);
    }
    
    public async Task<ShoppingCart> GetByIdAsync(Guid shoppingCartId)
    {
        var db = _redis.GetDatabase();

        var kartKey = GetShoppingCartKey(shoppingCartId.ToString());

        var jsonValue = await db.StringGetAsync(kartKey);

        if (string.IsNullOrEmpty(jsonValue.ToString()))
            return default;

        return JsonConvert.DeserializeObject<ShoppingCart>(jsonValue);
    }

    public async Task<Guid> GetActiveShoppingCartByClientIdAsync(Guid clientId)
    {
        var db = _redis.GetDatabase();

        var kartKey = GetClientShoppingCartKey(clientId);

        var jsonValue = await db.StringGetAsync(kartKey);

        if (string.IsNullOrEmpty(jsonValue.ToString()))
            return default;

        return JsonConvert.DeserializeObject<Guid>(jsonValue);
    }
    
    public async Task SetClientActiveShoppingCartAsync(Guid clientId, Guid shoppingCartId)
    {
        var db = _redis.GetDatabase();

        var kartKey = GetClientShoppingCartKey(clientId);

        string jsonValue = JsonConvert.SerializeObject(shoppingCartId);

        await db.StringSetAsync(kartKey, jsonValue, new TimeSpan(0, 0, 1200));
    }

    private static string GetClientShoppingCartKey(Guid clientId)
    {
        return $"{ClientKeyPrefix}:{clientId}";
    }

    private static string GetShoppingCartKey(string shoppingCartId)
    {
        return $"{ShoppingCartKeyPrefix}:{shoppingCartId}";
    }
}