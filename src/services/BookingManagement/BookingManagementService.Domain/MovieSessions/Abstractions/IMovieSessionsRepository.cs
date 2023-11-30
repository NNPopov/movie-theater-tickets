using System.Linq.Expressions;

namespace CinemaTicketBooking.Domain.MovieSessions.Abstractions
{
    public interface IMovieSessionsRepository
    {
        Task<MovieSession> MovieSession(MovieSession movieSession, CancellationToken cancel);

        Task<IEnumerable<MovieSession>> GetAllAsync(Expression<Func<MovieSession, bool>> filter,
            CancellationToken cancel);
        
        Task<MovieSession> GetByIdAsync(Guid movieSessionId,
            CancellationToken cancel);
        
    }
}