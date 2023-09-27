using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.Common.Ensure;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;


namespace CinemaTicketBooking.Infrastructure.Services;

public class DistributedLock : IDistributedLock
{
    private readonly IDatabase _database;

    private readonly ILogger<DistributedLock> _logger;

    public DistributedLock(IConnectionMultiplexer redis, ILogger<DistributedLock> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }


    public async Task<ILockHandler> TryAcquireAsync(string key,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        if (timeout == default)
            timeout = new TimeSpan(0, 0, 5);


        var result = await _database.StringSetAsync(key, key, timeout, When.NotExists, CommandFlags.DemandMaster);
        _logger.Log(LogLevel.Information, $"Key {key} was locked");
        return new LockHandler(_database, key, result, _logger);
    }
}

public class LockHandler : ILockHandler
{
    private readonly IDatabase _database;
    private readonly string _lockKey;
    private readonly ILogger _logger;

    public bool IsLocked { get; }

    public LockHandler(IDatabase database, string lockKey, bool lockResult, ILogger logger)
    {
        Ensure.NotEmpty(lockKey, "The lockKey is required.", nameof(lockKey));

        _database = database;
        IsLocked = lockResult;
        _lockKey = lockKey;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        if (IsLocked)
        {
            await _database.KeyDeleteAsync(_lockKey);
            _logger.Log(LogLevel.Information, $"Key {_lockKey} has been released");
        }
    }
}