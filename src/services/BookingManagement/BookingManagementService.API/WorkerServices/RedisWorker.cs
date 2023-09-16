using CinemaTicketBooking.Application.ShoppingCarts.Command.ExpiredSeatSelection;
using MediatR;
using StackExchange.Redis;
using ILogger = Serilog.ILogger;


namespace CinemaTicketBooking.Api.WorkerServices;

public sealed class RedisWorker : IHostedLifecycleService 
{
    private readonly ILogger _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RedisWorker(ILogger logger, IConnectionMultiplexer redis, IServiceScopeFactory serviceScopeFactory)
    {
        _redis = redis;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected  async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Subscriber();

        while (!stoppingToken.IsCancellationRequested)
        {
            //_logger.Information("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1_000, stoppingToken);
        }
    }

    private void Subscriber()
    {
        string EXPIRED_KEYS_CHANNEL = "__keyevent@0__:expired";

        try
        {
            ISubscriber subscriber = _redis.GetSubscriber();

            var mediator = _serviceScopeFactory
                .CreateScope()
                .ServiceProvider
                .GetRequiredService<IMediator>();

            subscriber.Subscribe(EXPIRED_KEYS_CHANNEL, async (channel, key) =>
            {
                var keys = key.ToString().Split(':');

                var keyPrefix = keys[0];

                switch (keyPrefix)
                {
                    case "seat-select":
                        var showtimeId = Guid.Parse(keys[1]);
                        var row = short.Parse(keys[2]);
                        var seat = short.Parse(keys[3]);
                        // var shopingKartId = Guid.Parse(keys[4]);

                        var seatExpiredReservationEvent = new SeatExpiredSelectionEvent(
                            MovieSessionId: showtimeId,
                            SeatRow: row,
                            SeatNumber: seat,
                            ShoppingKartId: Guid.Empty); // shopingKartId);

                        await mediator.Publish(seatExpiredReservationEvent);
                        //Console.WriteLine($"EXPIRED: {key}");
                        _logger.Warning($"EXPIRED: {key}");
                        break;
                    default:
                        break;
                }

            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            //throw;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        Subscriber();
        
        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}