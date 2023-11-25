using CinemaTicketBooking.Infrastructure.EventBus;

namespace CinemaTicketBooking.Api.IntegrationEvents.Events;

public record ShoppingCartExpiredIntegrationEvent
    (Guid ShoppingCartId) : IntegrationEvent;