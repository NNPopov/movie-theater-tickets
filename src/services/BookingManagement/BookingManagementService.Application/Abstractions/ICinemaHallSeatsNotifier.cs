using CinemaTicketBooking.Application.MovieSessions.Queries;

namespace CinemaTicketBooking.Application.Abstractions;

public interface ICinemaHallSeatsNotifier
{
    Task SendCinemaHallSeatsState(Guid movieSession,
        ICollection<MovieSessionSeatDto> seats);
}