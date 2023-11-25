using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure.Services;

public class ShoppingCartRepository : IShoppingCartRepository
{
    private readonly IConnectionMultiplexer _redis;

    // private readonly IMediator _mediator;

    private const string KeyPrefix = "cart";
    
    private const string KeyPrefixTimeToLive = "shopping_cart_ttl";
    
    private const string KeyClientPrefix = "client_session";
    
    private readonly IDomainEventTracker _domainEventTracker;

    public ShoppingCartRepository(IConnectionMultiplexer redis,
     //   IMediator mediator,
        IDomainEventTracker domainEventTracker)
    {
        _redis = redis;
     //   _mediator = mediator;
        _domainEventTracker = domainEventTracker;
    }

    // private async Task PublishDomainEvents(IAggregateRoot shoppingCart, CancellationToken cancellationToken = default)
    // {
    //     var domainEvents = shoppingCart.GetDomainEvents();
    //
    //
    //
    //     IEnumerable<Task> tasks = domainEvents.Select(domainEvent =>
    //     {
    //         var baseApplicationEventBuilder = typeof(BaseApplicationEvent<>).MakeGenericType(domainEvent.GetType());
    //
    //         var appEvent = Activator.CreateInstance(baseApplicationEventBuilder,
    //             domainEvent
    //         );
    //
    //         return  _mediator.Publish(appEvent, cancellationToken);
    //     });
    //     
    //
    //
    //     await Task.WhenAll(tasks);
    //     
    //     shoppingCart.ClearDomainEvents();
    // }

    public async Task<ShoppingCart> SetAsync(ShoppingCart shoppingCart)
    {
        var db = _redis.GetDatabase();

        var kartKey = $"{KeyPrefix}:{shoppingCart.Id.ToString()}";

        string jsonValue = JsonConvert.SerializeObject(shoppingCart);

        await db.StringSetAsync(kartKey, jsonValue, new TimeSpan(0, 0, 3600));
        
        var timeToLiveKey = $"{KeyPrefixTimeToLive}:{shoppingCart.Id.ToString()}";   
        await db.StringSetAsync(timeToLiveKey, timeToLiveKey, new TimeSpan(0, 0, 200));
        
        await _domainEventTracker.PublishDomainEvents(shoppingCart);

        return shoppingCart!;
    }

    public async Task DeleteAsync(ShoppingCart shoppingCart)
    {
        var db = _redis.GetDatabase();

        var kartKey = $"{KeyPrefix}:{shoppingCart.Id.ToString()}";

        await db.KeyDeleteAsync(kartKey);
        
        var timeToLiveKey = $"{KeyPrefixTimeToLive}:{shoppingCart.Id.ToString()}";   
        await db.KeyDeleteAsync(timeToLiveKey);

        await _domainEventTracker.PublishDomainEvents(shoppingCart);
    }

    public async Task<ShoppingCart> GetByIdAsync(Guid cartId)
    {
        var db = _redis.GetDatabase();

        var kartKey = $"{KeyPrefix}:{cartId}";

        var jsonValue = await db.StringGetAsync(kartKey);

        if (string.IsNullOrEmpty(jsonValue.ToString()))
            return default;

        return JsonConvert.DeserializeObject<ShoppingCart>(jsonValue);
    }

    public async Task<Guid> GetActiveShoppingCartByClientIdAsync(Guid clientId)
    {
        var db = _redis.GetDatabase();

        var kartKey = $"{KeyClientPrefix}:{clientId}";

        var jsonValue = await db.StringGetAsync(kartKey);

        if (string.IsNullOrEmpty(jsonValue.ToString()))
            return default;

        return JsonConvert.DeserializeObject<Guid>(jsonValue);
    }
    
    public async Task SetClientActiveShoppingCartAsync(Guid clientId, Guid shoppingCartId)
    {
        var db = _redis.GetDatabase();

        var kartKey = $"{KeyClientPrefix}:{clientId}";

        string jsonValue = JsonConvert.SerializeObject(shoppingCartId);

        await db.StringSetAsync(kartKey, jsonValue, new TimeSpan(0, 0, 1200));
    }
}