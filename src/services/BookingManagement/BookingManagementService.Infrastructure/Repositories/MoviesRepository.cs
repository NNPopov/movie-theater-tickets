using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.Movies;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Repositories;

public class MoviesRepository : IMoviesRepository
{
    private readonly CinemaContext _context;
    private readonly ICacheService _cacheService;

    public MoviesRepository(CinemaContext context, 
        ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    
    //Cache strategy Read Through
    public async Task<Movie> GetByIdAsync(Guid movieId, CancellationToken cancel)
    {
        var movieKey = $"movieId:{movieId}";
        Movie? movie =
            await _cacheService.TryGet<Movie>(movieKey);

        if (movie is null)
        {
            movie = await _context.Movies
                .FirstOrDefaultAsync(x => x.Id == movieId, cancel);

            if (movie is null )
                return default;


            await _cacheService.Set(movieKey, movie, new TimeSpan(1, 0, 0));
        }

        return movie;
    }

    public async Task<IReadOnlyCollection<Movie>> GetAllAsync(CancellationToken cancel)
    {
        
        return await _context.Movies.ToListAsync(cancel);
    }
}