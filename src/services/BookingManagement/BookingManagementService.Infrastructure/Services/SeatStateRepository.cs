using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure.Services;

public class SeatStateRepository : ISeatStateRepository
{
    private readonly IConnectionMultiplexer _redis;

    private const string KeyPrefix = "seat-select";


    public SeatStateRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<SeatShoppingCart> GetAsync(Guid movieSessionId, short seatRow, short seatNumber)
    {
        var db = _redis.GetDatabase();

        var key = GetKey(movieSessionId, seatRow, seatNumber);

        string jsonValue = await db.StringGetAsync(key);

        if (string.IsNullOrEmpty(jsonValue))
            return default;

        return JsonConvert.DeserializeObject<SeatShoppingCart>(jsonValue);
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

    public async Task<bool> SetAsync(Guid movieSessionId, short seatRow, short seatNumber, TimeSpan? expiry)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(movieSessionId, seatRow, seatNumber);

        string jsonValue = JsonConvert.SerializeObject(new SeatShoppingCart(seatRow, seatNumber));

        return await db.StringSetAsync(key, jsonValue, expiry);
    }
    
    
}
