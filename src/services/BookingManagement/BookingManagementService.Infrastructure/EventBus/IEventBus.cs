namespace CinemaTicketBooking.Infrastructure.EventBus;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent @event, string? deduplicationHeader = null);

    Task SubscribeAsync<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;

    void Unsubscribe<T, TH>()
        where TH : IIntegrationEventHandler<T>
        where T : IntegrationEvent;
}
