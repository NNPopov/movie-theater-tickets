using CinemaTicketBooking.Application.MovieSessions.Queries;

namespace CinemaTicketBooking.Application.Abstractions;

public interface ICinemaHallSeatsNotifier
{
    Task UpdateAndNotifySubscribersAboutSeatUpdates(Guid movieSessionId);

    Task SendSeatUpdatesDataToSpecificClient(Guid movieSessionId, string connectionId);
}