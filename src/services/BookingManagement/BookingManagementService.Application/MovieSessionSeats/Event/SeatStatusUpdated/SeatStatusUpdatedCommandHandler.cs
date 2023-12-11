using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Events;
using Serilog;

namespace CinemaTicketBooking.Application.MovieSessionSeats.Event.SeatStatusUpdated;

internal sealed class
    SeatStatusUpdatedCommandHandler : INotificationHandler<
    BaseApplicationEvent<MovieSessionSeatStatusUpdatedDomainEvent>>
{
    private readonly ICinemaHallSeatsNotifier _cinemaHallSeatsNotifier;
    private readonly ILogger _logger;

    public SeatStatusUpdatedCommandHandler(
        ICinemaHallSeatsNotifier cinemaHallSeatsNotifier, 
        ILogger logger)
    {
        _cinemaHallSeatsNotifier = cinemaHallSeatsNotifier;
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
              
                await _cinemaHallSeatsNotifier.UpdateAndNotifySubscribersAboutSeatUpdates(eventBody.MovieSessionId);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unable to update seat status:{@MovieSessionSeatStatusUpdatedDomainEvent}", request);
        }
    }
}