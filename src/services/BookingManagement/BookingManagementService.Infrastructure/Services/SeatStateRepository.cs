using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
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