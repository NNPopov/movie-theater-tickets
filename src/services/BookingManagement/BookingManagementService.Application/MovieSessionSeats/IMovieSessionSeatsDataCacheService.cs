using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.MovieSessionSeats.Queries;

namespace CinemaTicketBooking.Application.MovieSessionSeats;

public interface IMovieSessionSeatsDataCacheService
{
    Task AddOrUpdateMovieSessionSeatsCache(ActiveMovieSessionSeatsDTO data);
    Task<ActiveMovieSessionSeatsDTO?> GetActualMovieSessionSeatsData(Guid movieSessionId);
    Task<ActiveMovieSessionSeatsDTO?> GetMovieSessionSeatsData(Guid movieSessionId);
}