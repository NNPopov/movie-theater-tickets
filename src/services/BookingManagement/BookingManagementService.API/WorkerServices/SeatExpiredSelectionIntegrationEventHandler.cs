using CinemaTicketBooking.Application.ShoppingCarts.Command.ExpiredSeatSelection;
using CinemaTicketBooking.Infrastructure.EventBus;
using MediatR;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.WorkerServices;

public class SeatExpiredSelectionIntegrationEventHandler(IMediator mediator,
    ILogger logger) : IIntegrationEventHandler<SeatExpiredSelectionIntegrationEvent>
{
    private readonly ILogger _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

    /// <summary>
    /// Event handler which confirms that the grace period
    /// has been completed and order will not initially be cancelled.
    /// Therefore, the order process continues for validation. 
    /// </summary>
    /// <param name="event">       
    /// </param>
    /// <returns></returns>
    public async Task Handle(SeatExpiredSelectionIntegrationEvent @event)
    {
        _logger.Information("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);
            
            
        var seatExpiredReservationEvent = new SeatExpiredSelectionEvent(
            MovieSessionId: @event.MovieSessionId,
            SeatRow: @event.SeatRow,
            SeatNumber: @event.SeatNumber,
            ShoppingKartId: Guid.Empty);
            
        _logger.Debug(
            "Sending command: {@SeatExpiredReservationEvent})", seatExpiredReservationEvent);

        await mediator.Publish(seatExpiredReservationEvent);
    }
}