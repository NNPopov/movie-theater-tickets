using CinemaTicketBooking.Api.IntegrationEvents.Events;
using CinemaTicketBooking.Api.WorkerServices;
using CinemaTicketBooking.Application.ShoppingCarts.Command.ExpiredSeatSelection;
using CinemaTicketBooking.Application.ShoppingCarts.Command.ShoppingCartExpired;
using CinemaTicketBooking.Infrastructure.EventBus;
using MediatR;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.IntegrationEvents.EventHandling;

public class ShoppingCartExpiredEventHandler(IMediator mediator,
    ILogger logger) : IIntegrationEventHandler<ShoppingCartExpiredIntegrationEvent>
{
    private readonly ILogger _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

    /// <summary>
    /// Event handler - the shopping card has expired 
    /// </summary>
    /// <param name="integrationEvent">       
    /// </param>
    /// <returns></returns>
    public async Task Handle(ShoppingCartExpiredIntegrationEvent integrationEvent)
    {
        _logger.Information("Handling integration integrationEvent: {IntegrationEventId} - ({@IntegrationEvent})", integrationEvent.Id, integrationEvent);
            
            
        var seatExpiredReservationEvent = new ShoppingCartExpiredCommand(
            ShoppingCartId: integrationEvent.ShoppingCartId);
            
        _logger.Debug(
            "Sending command: {@SeatExpiredReservationEvent})", seatExpiredReservationEvent);

        await mediator.Publish(seatExpiredReservationEvent);
    }
}