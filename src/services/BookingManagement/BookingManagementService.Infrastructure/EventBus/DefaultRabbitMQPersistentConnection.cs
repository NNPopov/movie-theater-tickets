using System.Net.Sockets;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Serilog;

namespace CinemaTicketBooking.Infrastructure.EventBus;


public class DefaultRabbitMQPersistentConnection
    : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger _logger;
    private readonly int _retryCount;
    private IConnection _connection;
    public bool Disposed;

    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory,
        ILogger logger, int retryCount = 5)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryCount = retryCount;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !Disposed;

    public async Task<IChannel> CreateChannelAsync()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return await _connection.CreateChannelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Disposed) return;

        Disposed = true;

        try
        {
            if (_connection is not null)
            {
                _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
                _connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
                _connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
                await _connection.DisposeAsync();
            }
        }
        catch (IOException ex)
        {
            _logger.Error(ex.ToString(), $"{nameof(DefaultRabbitMQPersistentConnection)} Dispose Filed");
        }
    }

    public async Task<bool> TryConnectAsync()
    {
        _logger.Information("RabbitMQ Client is trying to connect");

        await _connectionLock.WaitAsync();

        try
        {
            AsyncRetryPolicy policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.Warning(ex, "RabbitMQ Client could not connect after {TimeOut}s", $"{time.TotalSeconds:n1}");
                }
            );

            await policy.ExecuteAsync(async () =>
            {
                _connection = await _connectionFactory
                        .CreateConnectionAsync();
            });

            if (IsConnected)
            {
                _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
                _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;

                _logger.Information("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

                return true;
            }
            else
            {
                _logger.Information("Fatal error: RabbitMQ connections could not be created and opened");

                return false;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
    {
        if (Disposed) return;

        _logger.Warning("A RabbitMQ connection is shutdown. Trying to re-connect...");

        await TryConnectAsync();
    }

    private async Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
    {
        if (Disposed) return;

        _logger.Warning("A RabbitMQ connection throw exception. Trying to re-connect...");

        await TryConnectAsync();
    }

    private async Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs reason)
    {
        if (Disposed) return;

        _logger.Warning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

        await TryConnectAsync();
    }
}
