using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure.Services;

public class ShoppingCartSeatLifecycleManager : IShoppingCartSeatLifecycleManager
{
    private readonly IConnectionMultiplexer _redis;

    private const string KeyPrefix = "seat-select";


    public ShoppingCartSeatLifecycleManager(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> IsSeatReservedAsync(Guid movieSessionId, short seatRow, short seatNumber)
    {
        var db = _redis.GetDatabase();

        var key = GetKey(movieSessionId, seatRow, seatNumber);

        string jsonValue = await db.StringGetAsync(key);

        if (string.IsNullOrEmpty(jsonValue))
            return false;

        return true;
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



    public async Task<bool> SetAsync(Guid movieSessionId, SeatShoppingCart seatShoppingCart)
   
    {
        var db = _redis.GetDatabase();
        var key = GetKey(movieSessionId, seatShoppingCart.SeatRow, seatShoppingCart.SeatNumber);

        var expiryTimeSpan = seatShoppingCart.SelectionExpirationTime.Value.Subtract(TimeProvider.System.GetUtcNow().DateTime);
        
        string jsonValue = JsonConvert.SerializeObject(seatShoppingCart);

        return await db.StringSetAsync(key, jsonValue, expiryTimeSpan);
    }

    public async Task<bool> SetAsync(Guid movieSessionId, Guid shoppingCartId, short seatRow, short seatNumber,
        DateTime expires)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(movieSessionId, seatRow, seatNumber);

        var expiryTimeSpan = expires.Subtract(TimeProvider.System.GetUtcNow().DateTime);
        
        string jsonValue = JsonConvert.SerializeObject(shoppingCartId);

        return await db.StringSetAsync(key, jsonValue, expiryTimeSpan);
    }
}