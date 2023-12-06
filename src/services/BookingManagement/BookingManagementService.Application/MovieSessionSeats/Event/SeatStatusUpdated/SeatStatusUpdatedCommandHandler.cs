using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Seats.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Serilog;

namespace CinemaTicketBooking.Application.MovieSessionSeats.Event.SeatStatusUpdated;

internal sealed class
    SeatStatusUpdatedCommandHandler : INotificationHandler<
    BaseApplicationEvent<MovieSessionSeatStatusUpdatedDomainEvent>>
{
    private readonly ICinemaHallSeatsNotifier _cinemaHallSeatsNotifier;
    private readonly ISender _sender;
    private readonly ILogger _logger;

    public SeatStatusUpdatedCommandHandler(
        ICinemaHallSeatsNotifier cinemaHallSeatsNotifier, ISender sender, ILogger logger)
    {
        _cinemaHallSeatsNotifier = cinemaHallSeatsNotifier;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(BaseApplicationEvent<MovieSessionSeatStatusUpdatedDomainEvent> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var eventBody = request.Event as MovieSessionSeatStatusUpdatedDomainEvent;

            if (eventBody == null)
            {
                _logger.Error("Unable to cast event to {@MovieSessionSeatStatusUpdatedDomainEvent}", request);
                return;
            }

            bool isStatusChanged = eventBody.CurrentStatus != eventBody.PreviousStatus;
            if (isStatusChanged && (eventBody.CurrentStatus == SeatStatus.Selected ||
                                    eventBody.CurrentStatus == SeatStatus.Available))
            {
                var query = new GetMovieSessionSeatsQuery(eventBody.MovieSessionId);
                var movieSessionSeat = await _sender.Send(query, cancellationToken);


                await _cinemaHallSeatsNotifier.SentCinemaHallSeatsState(eventBody.MovieSessionId, movieSessionSeat);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unable to update seat status:{@MovieSessionSeatStatusUpdatedDomainEvent}", request);
        }
    }
}