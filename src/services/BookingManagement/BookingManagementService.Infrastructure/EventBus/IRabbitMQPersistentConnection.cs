using RabbitMQ.Client;

namespace CinemaTicketBooking.Infrastructure.EventBus;

public interface IRabbitMQPersistentConnection
    : IAsyncDisposable
{
    bool IsConnected { get; }

    Task<bool> TryConnectAsync();

    Task<IChannel> CreateChannelAsync();
}
