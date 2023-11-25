using CinemaTicketBooking.Infrastructure.EventBus;

namespace CinemaTicketBooking.Api.IntegrationEvents.Events;

public record SeatExpiredSelectionIntegrationEvent
    (Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingKartId) : IntegrationEvent;