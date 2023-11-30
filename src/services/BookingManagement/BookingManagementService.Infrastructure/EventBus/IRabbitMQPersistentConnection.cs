using RabbitMQ.Client;

namespace CinemaTicketBooking.Infrastructure.EventBus;

public interface IRabbitMQPersistentConnection
    : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect();

    IModel CreateModel();
}