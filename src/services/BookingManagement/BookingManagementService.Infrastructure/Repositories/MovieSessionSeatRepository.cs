using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Repositories;

public class MovieSessionSeatRepository : IMovieSessionSeatRepository
{
    private readonly CinemaContext _context;

    public MovieSessionSeatRepository(CinemaContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MovieSessionSeat movieSessionSeat, CancellationToken cancellationToken)
    {
        _context.ShowtimeSeats.Add(movieSessionSeat);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MovieSessionSeat movieSessionSeat, CancellationToken cancellationToken)
    {
        _context.ShowtimeSeats.Update(movieSessionSeat);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ICollection<MovieSessionSeat>> GetByMovieSessionIdAsync(Guid movieSessionId,
        CancellationToken cancellationToken)
    {
        return await _context.ShowtimeSeats.Where(t => t.MovieSessionId == movieSessionId).ToListAsync(cancellationToken);
    }

    public async Task<MovieSessionSeat?> GetByIdAsync(Guid movieSessionId, short seatRow, short seatNumber,
        CancellationToken cancellationToken)
    {
        return await _context.ShowtimeSeats.FirstOrDefaultAsync(t => t.MovieSessionId == movieSessionId &&
                                                                     t.SeatRow == seatRow &&
                                                                     t.SeatNumber == seatNumber,
            cancellationToken);
    }
}