namespace CinemaTicketBooking.Infrastructure.EventBus;

public interface IEventBus
{
    void Publish(IntegrationEvent @event, string? deduplicationHeader = null);

    void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;

    void Unsubscribe<T, TH>()
        where TH : IIntegrationEventHandler<T>
        where T : IntegrationEvent;
}