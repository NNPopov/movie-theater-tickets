using CinemaTicketBooking.Application.ShoppingCarts.Command.ExpiredSeatSelection;
using MediatR;
using StackExchange.Redis;
using ILogger = Serilog.ILogger;


namespace CinemaTicketBooking.Api.WorkerServices;

public sealed class RedisSubscriber :BackgroundService
{
    private readonly ILogger _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private ISubscriber subscriber;
    private IMediator mediator;
    public RedisSubscriber(ILogger logger, IConnectionMultiplexer redis, IServiceScopeFactory serviceScopeFactory)
    {
        _redis = redis;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        Subscriber();
        
        subscriber = _redis.GetSubscriber();
        
                    mediator = _serviceScopeFactory
                        .CreateScope()
                        .ServiceProvider
                        .GetRequiredService<IMediator>();
    }

    private void Subscriber()
    {
       
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string EXPIRED_KEYS_CHANNEL = "__keyevent@0__:expired";

        //string EXPIRED_KEYS_CHANNEL = "eventList";
        
       
            try
                 {
                     await subscriber.SubscribeAsync(EXPIRED_KEYS_CHANNEL, async (channel, key) =>
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
                                 
                                 _logger.Error($"EXPIRED: {key}");
                                 break;
                         }
         
                     });
                 }
                 catch (Exception e)
                 {      _logger.Error("EXPIRED: {e}", e);
         
                 }

             
            while (!stoppingToken.IsCancellationRequested)
            {
            
        //    _logger.Error("Redis IsConnected: {@IsConnected}, status {@GetStatus}", subscriber.IsConnected(), _redis.GetStatus());
            await Task.Delay(1000, stoppingToken);
        }
    }
}