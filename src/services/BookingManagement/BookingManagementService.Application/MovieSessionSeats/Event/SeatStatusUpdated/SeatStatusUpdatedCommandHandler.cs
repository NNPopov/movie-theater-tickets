using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Seats.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.MovieSessionSeats.Event.SeatStatusUpdated;

public class
    SeatStatusUpdatedCommandHandler : INotificationHandler<BaseApplicationEvent<MovieSessionSeatStatusUpdatedDomainEvent>>
{
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;

    private readonly ICinemaHallSeatsNotifier _cinemaHallSeatsNotifier;
    private readonly ISender _sender;

    public SeatStatusUpdatedCommandHandler(
        IMovieSessionSeatRepository movieSessionSeatRepository,
        ICinemaHallSeatsNotifier cinemaHallSeatsNotifier, ISender sender)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _cinemaHallSeatsNotifier = cinemaHallSeatsNotifier;
        _sender = sender;
    }

    public async Task Handle(BaseApplicationEvent<MovieSessionSeatStatusUpdatedDomainEvent> request,
        CancellationToken cancellationToken)
    {
        var eventBody = (MovieSessionSeatStatusUpdatedDomainEvent)request.Event;

        if (eventBody.CurrentStatus == SeatStatus.Selected && eventBody.PreviousStatus== SeatStatus.Available
            
            ||
            eventBody.CurrentStatus == SeatStatus.Available && eventBody.PreviousStatus== SeatStatus.Selected
            )
        {
            var query = new GetMovieSessionSeatsQuery(eventBody.MovieSessionId);
            var movieSessionSeat = await _sender.Send(query, cancellationToken);
            
            
            await _cinemaHallSeatsNotifier.SentCinemaHallSeatsState(eventBody.MovieSessionId,movieSessionSeat);
        }
        
    }
}