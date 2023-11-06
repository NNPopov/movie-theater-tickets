using CinemaTicketBooking.Domain.Entities;
using CinemaTicketBooking.Domain.Movies;

namespace CinemaTicketBooking.Application.Abstractions
{
    public interface IMoviesRepository
    {
        Task<Movie> GetByIdAsync(Guid movieId, CancellationToken cancel);
        
        Task<IReadOnlyCollection<Movie>> GetAllAsync( CancellationToken cancel);
    }
}