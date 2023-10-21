using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.ShoppingCarts;
using MediatR;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure.Services;

public class ShoppingCartRepository : IShoppingCartRepository
{
    private readonly IConnectionMultiplexer _redis;

    private readonly IMediator _mediator;

    private const string KeyPrefix = "cart";

    public ShoppingCartRepository(IConnectionMultiplexer redis, IMediator mediator)
    {
        _redis = redis;
        _mediator = mediator;
    }

    private async Task PublishDomainEvents(IAggregateRoot shoppingCart, CancellationToken cancellationToken = default)
    {
        var domainEvents = shoppingCart.DomainEvents;

        

        IEnumerable<Task> tasks = domainEvents.Select(domainEvent =>
        {
            var baseApplicationEventBuilder = typeof(BaseApplicationEvent<>).MakeGenericType(domainEvent.GetType());

            var appEvent = Activator.CreateInstance(baseApplicationEventBuilder,
                domainEvent
            );

            return  _mediator.Publish(appEvent, cancellationToken);
        });
        


        await Task.WhenAll(tasks);
        
        shoppingCart.ClearDomainEvents();
    }

    public async Task<ShoppingCart> TrySetCart(ShoppingCart shoppingCart)
    {
        var db = _redis.GetDatabase();

        var kartKey = $"{KeyPrefix}:{shoppingCart.Id.ToString()}";

        string jsonValue = JsonConvert.SerializeObject(shoppingCart);

        await db.StringSetAsync(kartKey, jsonValue, new TimeSpan(0, 0, 1200));

        await PublishDomainEvents(shoppingCart);

        return shoppingCart!;
    }

    public async Task<ShoppingCart> TryGetCart(Guid cartId)
    {
        var db = _redis.GetDatabase();

        var kartKey = $"{KeyPrefix}:{cartId}";

        var jsonValue = await db.StringGetAsync(kartKey);

        if (string.IsNullOrEmpty(jsonValue.ToString()))
            return default;

        return JsonConvert.DeserializeObject<ShoppingCart>(jsonValue);
    }
}

public class SeatStateRepository : ISeatStateRepository
{
    private readonly IConnectionMultiplexer _redis;

    private const string KeyPrefix = "seat-select";


    public SeatStateRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }


    public async Task<ICollection<SeatDto>> GetReservedSeats(Guid showtimeId)
    {
        var db = _redis.GetDatabase();
        var reservedSeatsKey = await db.ExecuteAsync("KEYS", $"{KeyPrefix}:{showtimeId.ToString()}:*");

        var re = (RedisResult[])reservedSeatsKey;
        var response = re.Select(t =>
        {
            var key = t.ToString().Split(':');
            return new SeatDto(Row: short.Parse(key[2]), Number: short.Parse(key[3]));
        });
        return response.ToList();
    }


    public async Task<string> StringGetAsync(string key)
    {
        var db = _redis.GetDatabase();

        var value = await db.StringGetAsync(key);


        return value;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var db = _redis.GetDatabase();

        string jsonValue = await db.StringGetAsync(key);

        if (string.IsNullOrEmpty(jsonValue))
            return default;

        var value = JsonConvert.DeserializeObject<T>(jsonValue);
        return value;
    }

    public async Task<SeatSelectedInfo> GetAsync(Guid movieSessionId, short seatRow, short seatNumber)
    {
        var db = _redis.GetDatabase();

        var key = GetKey(movieSessionId, seatRow, seatNumber);

        string jsonValue = await db.StringGetAsync(key);

        if (string.IsNullOrEmpty(jsonValue))
            return default;

        return JsonConvert.DeserializeObject<SeatSelectedInfo>(jsonValue);
    }


    public async Task DeleteAsync(Guid movieSessionId, short seatRow, short seatNumber)
    {
        var key = GetKey(movieSessionId, seatRow, seatNumber);

        var db = _redis.GetDatabase();

        await db.KeyDeleteAsync(key);
    }

    private static string GetKey(Guid movieSessionId, short seatRow, short seatNumber)
    {
        return $"{KeyPrefix}:{movieSessionId.ToString()}:{seatRow}:{seatNumber}";
    }


    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry)
    {
        var db = _redis.GetDatabase();
        // var opts = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles};


        string jsonValue = JsonConvert.SerializeObject(value);

        return await db.StringSetAsync(key, jsonValue, expiry, When.NotExists);
    }


    public async Task<bool> SetAsync(SeatSelectedInfo value, TimeSpan? expiry)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(value.MovieSessionId, value.SeatRow, value.SeatNumber);

        string jsonValue = JsonConvert.SerializeObject(value);

        return await db.StringSetAsync(key, jsonValue, expiry);
    }

    public async Task<bool> SetAsync<T>(string key, T value)
    {
        var db = _redis.GetDatabase();
        //var opts = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles};

        string jsonValue = JsonConvert.SerializeObject(value);

        return await db.StringSetAsync(key, jsonValue);
    }

    public async Task<bool> StringSetIfNotExistsAsync(string key, string value, TimeSpan? expiry)
    {
        var db = _redis.GetDatabase();

        return await db.StringSetAsync(key, value, expiry, When.NotExists);
    }
}