using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Infrastructure.EventBus;
using ILogger = Serilog.ILogger;


namespace CinemaTicketBooking.Api.WorkerServices;

public sealed class TimeWorker : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServerStateNotifier _serverStateNotifier;


    public TimeWorker(ILogger logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        using var serviceScope = serviceScopeFactory.CreateScope();
        
        _serverStateNotifier = serviceScope.ServiceProvider.GetRequiredService<IServerStateNotifier>();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {

        while (!cancellationToken.IsCancellationRequested)
        {
            
            await _serverStateNotifier.SentServerState(new ServerState(TimeProvider.System.GetUtcNow().DateTime));
            await Task.Delay(5000, cancellationToken);
        }

        _logger.Information("Redis is disconnected");
    }
}