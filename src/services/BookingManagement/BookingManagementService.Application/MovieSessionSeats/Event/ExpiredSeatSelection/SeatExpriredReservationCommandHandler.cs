using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Serilog;

namespace CinemaTicketBooking.Application.MovieSessionSeats.Event.ExpiredSeatSelection;

public class MovieSessionSeatExpiredReservationEventHandler : INotificationHandler<BaseApplicationEvent<SeatRemovedFromShoppingCartDomainEvent>>
{
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly ILogger _logger;

    public MovieSessionSeatExpiredReservationEventHandler(
        IMovieSessionSeatRepository movieSessionSeatRepository,
        ILogger logger)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _logger = logger;
    }

    public async Task Handle(BaseApplicationEvent<SeatRemovedFromShoppingCartDomainEvent> request,
        CancellationToken cancellationToken)
    {
        var eventBody = (SeatRemovedFromShoppingCartDomainEvent)request.Event;
        var movieSessionSeat =
            await _movieSessionSeatRepository.GetByIdAsync(eventBody.MovieSessionId, eventBody.SeatRow, eventBody.SeatNumber, cancellationToken);

        if (movieSessionSeat is not null)
        {
            movieSessionSeat.ReturnToAvailable();
            await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);
            _logger.Information("MovieSessionSeat returned to Available:{@MovieSessionSeat}", movieSessionSeat);
        }
        else
        {
            _logger.Error("Couldnot find MovieSessionSeat, EventBody:{@eventBody}",
                eventBody);
        }
    }
}