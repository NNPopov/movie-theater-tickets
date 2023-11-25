using CinemaTicketBooking.Api.IntegrationEvents.Events;
using CinemaTicketBooking.Api.WorkerServices;
using CinemaTicketBooking.Application.ShoppingCarts.Command.ExpiredSeatSelection;
using CinemaTicketBooking.Infrastructure.EventBus;
using MediatR;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.IntegrationEvents.EventHandling;

public class SeatExpiredSelectionIntegrationEventHandler(IMediator mediator,
    ILogger logger) : IIntegrationEventHandler<SeatExpiredSelectionIntegrationEvent>
{
    private readonly ILogger _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

    /// <summary>
    /// Event handler for Expired the waiting time for the action of selecting seats in the cinema hall 
    /// </summary>
    /// <param name="event">       
    /// </param>
    /// <returns></returns>
    public async Task Handle(SeatExpiredSelectionIntegrationEvent @event)
    {
        _logger.Information("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);
            
            
        var command = new SeatExpiredSelectionCommand(
            MovieSessionId: @event.MovieSessionId,
            SeatRow: @event.SeatRow,
            SeatNumber: @event.SeatNumber,
            ShoppingKartId: Guid.Empty);
            
        _logger.Debug(
            "Sending command: {@SeatExpiredSelectionCommand})", command);

        await mediator.Publish(command);
    }
}