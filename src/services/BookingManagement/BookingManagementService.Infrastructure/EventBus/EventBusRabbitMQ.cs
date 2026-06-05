using System.Net.Sockets;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Serilog;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace CinemaTicketBooking.Infrastructure.EventBus;


public class EventBusRabbitMQ : IEventBus, IAsyncDisposable
{
    const string BROKER_NAME = "eshop_event_bus";

    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly ILogger _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _retryCount;

    private IChannel? _consumerChannel;
    private string _queueName;
    private readonly SemaphoreSlim _consumerChannelLock = new(1, 1);

    public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection,
        ILogger logger,
        IServiceProvider serviceProvider,
        IEventBusSubscriptionsManager subsManager,
        string queueName = null,
        int retryCount = 5)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
        _queueName = queueName;
        _serviceProvider = serviceProvider;
        _retryCount = retryCount;
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    private async void SubsManager_OnEventRemoved(object sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        await using var channel = await _persistentConnection.CreateChannelAsync();
        await channel.QueueUnbindAsync(queue: _queueName,
            exchange: BROKER_NAME,
            routingKey: eventName);

        if (_subsManager.IsEmpty)
        {
            _queueName = string.Empty;
            if (_consumerChannel is not null)
            {
                await _consumerChannel.CloseAsync();
            }
        }
    }

    public async Task PublishAsync(IntegrationEvent @event, string? deduplicationHeader = null)
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        AsyncRetryPolicy policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.Warning(ex, "Could not publish event: {EventId} after {Timeout}s", @event.Id, $"{time.TotalSeconds:n1}");
            });

        var eventName = @event.GetType().Name;

        _logger.Debug("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

        await using var channel = await _persistentConnection.CreateChannelAsync();
        _logger.Debug("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

        await channel.ExchangeDeclareAsync(exchange: BROKER_NAME, type: "direct");

        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

        await policy.ExecuteAsync(async () =>
        {
            var properties = new BasicProperties
            {
                Persistent = true
            };

            if (deduplicationHeader != null)
            {
                properties.Headers ??= new Dictionary<string, object?>
                    { { "x-deduplication-header", deduplicationHeader } };
            }

            _logger.Debug("Publishing event to RabbitMQ: {EventId}", @event.Id);

            await channel.BasicPublishAsync(
                exchange: BROKER_NAME,
                routingKey: eventName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });
    }

    public async Task SubscribeDynamicAsync<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        _logger.Information("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

        await DoInternalSubscriptionAsync(eventName);
        _subsManager.AddDynamicSubscription<TH>(eventName);
        await StartBasicConsumeAsync();
    }

    public async Task SubscribeAsync<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        await DoInternalSubscriptionAsync(eventName);

        _logger.Information("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

        _subsManager.AddSubscription<T, TH>();
        await StartBasicConsumeAsync();
    }

    private async Task DoInternalSubscriptionAsync(string eventName)
    {
        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
        if (!containsKey)
        {
            if (!_persistentConnection.IsConnected)
            {
                await _persistentConnection.TryConnectAsync();
            }

            var channel = await EnsureConsumerChannelAsync();
            await channel.QueueBindAsync(queue: _queueName,
                                exchange: BROKER_NAME,
                                routingKey: eventName);
        }
    }

    public void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();

        _logger.Information("Unsubscribing from event {EventName}", eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void UnsubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        _subsManager.RemoveDynamicSubscription<TH>(eventName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_consumerChannel != null)
        {
            await _consumerChannel.DisposeAsync();
        }

        _subsManager.Clear();
    }

    private async Task StartBasicConsumeAsync()
    {
        _logger.Debug("Starting RabbitMQ basic consume");

        var channel = await EnsureConsumerChannelAsync();

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += Consumer_Received;

        await channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }

            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error Processing message \"{Message}\"", message);
        }

        // Even on exception we take the message off the queue.
        // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX).
        // For more information see: https://www.rabbitmq.com/dlx.html
        if (_consumerChannel is not null)
        {
            await _consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
    }

    private async Task<IChannel> EnsureConsumerChannelAsync()
    {
        if (_consumerChannel is { IsOpen: true })
        {
            return _consumerChannel;
        }

        await _consumerChannelLock.WaitAsync();
        try
        {
            if (_consumerChannel is not { IsOpen: true })
            {
                _consumerChannel = await CreateConsumerChannelAsync();
            }

            return _consumerChannel;
        }
        finally
        {
            _consumerChannelLock.Release();
        }
    }

    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        _logger.Debug("Creating RabbitMQ consumer channel");

        var channel = await _persistentConnection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: BROKER_NAME,
                                type: "direct");

        await channel.QueueDeclareAsync(queue: _queueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

        channel.CallbackExceptionAsync += async (sender, ea) =>
        {
            _logger.Warning(ea.Exception, "Recreating RabbitMQ consumer channel");

            if (_consumerChannel is not null)
            {
                await _consumerChannel.DisposeAsync();
            }

            _consumerChannel = await CreateConsumerChannelAsync();
            await StartBasicConsumeAsync();
        };

        return channel;
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        _logger.Debug("Processing RabbitMQ event: {EventName}", eventName);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var subscriptions = _subsManager.GetHandlersForEvent(eventName);
            foreach (var subscription in subscriptions)
            {
                if (subscription.IsDynamic)
                {
                    if (scope.ServiceProvider.GetService(subscription.HandlerType) is not IDynamicIntegrationEventHandler handler) continue;
                    using dynamic eventData = JsonConvert.DeserializeObject(message);
                    await Task.Yield();
                    await handler.Handle(eventData);
                }
                else
                {
                    var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null) continue;
                    var eventType = _subsManager.GetEventTypeByName(eventName);
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                    await Task.Yield();
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                }
            }
        }
        else
        {
            _logger.Warning("No subscription for RabbitMQ event: {EventName}", eventName);
        }
    }
}
