using System.Linq.Expressions;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.Entities;
using CinemaTicketBooking.Domain.MovieSessions;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Repositories
{
    public class MovieSessionsRepository : IMovieSessionsRepository
    {
        private readonly CinemaContext _context;

        public MovieSessionsRepository(CinemaContext context)
        {
            _context = context;
        }

        public async Task<MovieSession> GetByIdAsync(Guid movieSessionId, CancellationToken cancel)
        {
            return await _context.MovieSessions
                .FirstOrDefaultAsync(x => x.Id == movieSessionId, cancel);
        }

        public async Task<MovieSession> GetWithMoviesByIdAsync(Guid id, CancellationToken cancel)
        {
            return await _context.MovieSessions
                .FirstOrDefaultAsync(x => x.Id == id, cancel);
        }

        public async Task<MovieSession> GetWithTicketsByIdAsync(Guid id, CancellationToken cancel)
        {
            return await _context.MovieSessions
                .FirstOrDefaultAsync(x => x.Id == id, cancel);
        }

        public async Task<IEnumerable<MovieSession>> GetAllAsync(Expression<Func<MovieSession, bool>> filter,
            CancellationToken cancel)
        {
            if (filter == null)
            {
                return await _context.MovieSessions
                //.Include(x => x.Movie)
                .ToListAsync(cancel);
            }
            return await _context.MovieSessions
                //.Include(x => x.Movie)
                .Where(filter)
                .ToListAsync(cancel);
        }

        public async Task<MovieSession> MovieSession(MovieSession movieSession, CancellationToken cancel)
        {
            var showtime = await _context.MovieSessions.AddAsync(movieSession, cancel);
            await _context.SaveChangesAsync(cancel);
            return showtime.Entity;
        }
    }
}
