namespace CinemaTicketBooking.Infrastructure.EventBus;

public interface IDynamicIntegrationEventHandler
{
    Task Handle(dynamic eventData);
}
