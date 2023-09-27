using System.Linq.Expressions;
using CinemaTicketBooking.Domain.Entities;
using CinemaTicketBooking.Domain.MovieSessions;

namespace CinemaTicketBooking.Application.Abstractions
{
    public interface IMovieSessionsRepository
    {
        Task<MovieSession> CreateShowtime(MovieSession movieSession, CancellationToken cancel);

        Task<IEnumerable<MovieSession>> GetAllAsync(Expression<Func<MovieSession, bool>> filter,
            CancellationToken cancel);

        Task<MovieSession> GetWithMoviesByIdAsync(Guid id, CancellationToken cancel);
        Task<MovieSession> GetWithTicketsByIdAsync(Guid id, CancellationToken cancel);
    }
}