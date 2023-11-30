using CinemaTicketBooking.Api.IntegrationEvents.Events;
using CinemaTicketBooking.Infrastructure.EventBus;
using StackExchange.Redis;
using ILogger = Serilog.ILogger;


namespace CinemaTicketBooking.Api.WorkerServices;

public sealed class RedisSubscriber : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly IEventBus _eventBus;

    public RedisSubscriber(ILogger logger,
        IConnectionMultiplexer redis,
        IServiceScopeFactory serviceScopeFactory,
        IEventBus eventBus)
    {
        _redis = redis;
        _eventBus = eventBus;
        _logger = logger;

        _subscriber = _redis.GetSubscriber();
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        string EXPIRED_KEYS_CHANNEL = "__keyevent@0__:expired";


        await _subscriber.SubscribeAsync(EXPIRED_KEYS_CHANNEL, async (channel, key) =>
        {
            try
            {
                var keys = key.ToString().Split(':');

                var keyPrefix = keys[0];

                switch (keyPrefix)
                {
                    case "seat-select":
                        var showtimeId = Guid.Parse(keys[1]);
                        var row = short.Parse(keys[2]);
                        var seat = short.Parse(keys[3]);

                        var seatExpiredReservationIntegrationEvent = new SeatExpiredSelectionIntegrationEvent(
                            MovieSessionId: showtimeId,
                            SeatRow: row,
                            SeatNumber: seat,
                            ShoppingKartId: Guid.Empty);

                        _eventBus.Publish(seatExpiredReservationIntegrationEvent, key.ToString());


                        _logger.Information(
                            "Seat Select EXPIRED. Message published: {@SeatExpiredReservationIntegrationEvent}",
                            seatExpiredReservationIntegrationEvent);
                        break;

                    case "shopping_cart_ttl":
                        var shoppingCartId = Guid.Parse(keys[1]);

                        var shoppingCartExpiredIntegrationEvent = new ShoppingCartExpiredIntegrationEvent(
                            ShoppingCartId: shoppingCartId);

                        _eventBus.Publish(shoppingCartExpiredIntegrationEvent, key.ToString());


                        _logger.Information(
                            "Shopping Cart EXPIRED. Message published: {@ShoppingCartExpiredIntegrationEvent}",
                            shoppingCartExpiredIntegrationEvent);
                        break;
                    default:

                        _logger.Error("Handler for the key was not found. Key: {@Кey}", key);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error processing message");
            }
        });


        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }

        _logger.Information("Redis is disconnected");
    }
}