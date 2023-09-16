using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.MovieSessionSeats.Event.ExpiredSeatSelection;

public class MovieSessionSeatExpiredReservationEventHandler : INotificationHandler<BaseApplicationEvent<SeatRemovedFromShoppingCartDomainEvent>>
{
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    

    public MovieSessionSeatExpiredReservationEventHandler(
        IMovieSessionSeatRepository movieSessionSeatRepository)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
    }

    public async Task Handle(BaseApplicationEvent<SeatRemovedFromShoppingCartDomainEvent> request,
        CancellationToken cancellationToken)
    {
        var eventBody = (SeatRemovedFromShoppingCartDomainEvent)request.Event;
        var movieSessionSeat =
            await _movieSessionSeatRepository.GetByIdAsync(eventBody.MovieSessionId, eventBody.SeatRow, eventBody.SeatNumber, cancellationToken);

        if (movieSessionSeat is not null)
        {
            movieSessionSeat.TryReturnToAvailable();
            await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);
        }
    }
}