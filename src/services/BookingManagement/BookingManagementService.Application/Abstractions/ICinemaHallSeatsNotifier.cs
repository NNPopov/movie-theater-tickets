using CinemaTicketBooking.Application.MovieSessions.Queries;

namespace CinemaTicketBooking.Application.Abstractions;

public interface ICinemaHallSeatsNotifier
{
    Task SentCinemaHallSeatsState(Guid movieSessionId,
        ICollection<MovieSessionSeatDto> seats);
}