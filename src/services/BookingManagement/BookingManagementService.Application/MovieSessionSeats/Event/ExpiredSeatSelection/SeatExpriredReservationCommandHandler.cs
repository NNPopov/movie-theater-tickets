using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Serilog;

namespace CinemaTicketBooking.Application.MovieSessionSeats.Event.ExpiredSeatSelection;

public class
    MovieSessionSeatExpiredReservationEventHandler : INotificationHandler<
    BaseApplicationEvent<SeatRemovedFromShoppingCartDomainEvent>>
{
    private readonly MovieSessionSeatService _movieSessionSeatService;
    private readonly ILogger _logger;

    public MovieSessionSeatExpiredReservationEventHandler(
        ILogger logger,
        MovieSessionSeatService movieSessionSeatService)
    {
        _logger = logger;
        _movieSessionSeatService = movieSessionSeatService;
    }

    public async Task Handle(BaseApplicationEvent<SeatRemovedFromShoppingCartDomainEvent> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var eventBody = request.Event as SeatRemovedFromShoppingCartDomainEvent;
            if (eventBody == null)
            {
                _logger.Error("Unable to cast event to {@SeatRemovedFromShoppingCartDomainEvent}", request);
                return;
            }

            await _movieSessionSeatService.ReturnToAvailable(eventBody.MovieSessionId,
                eventBody.SeatRow,
                eventBody.SeatNumber,
                cancellationToken
            );

            _logger.Debug("MovieSessionSeat returned to Available:{@MovieSessionSeat}", eventBody);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error returning seat to Available:{@SeatRemovedFromShoppingCartDomainEvent}", request);
        }
    }
}