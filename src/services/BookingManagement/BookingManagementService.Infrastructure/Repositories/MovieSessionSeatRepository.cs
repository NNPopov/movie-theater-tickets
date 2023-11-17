using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CinemaTicketBooking.Infrastructure.Repositories;

public class MovieSessionSeatRepository(CinemaContext context,
        ILogger logger,
        IDomainEventTracker domainEventTracker)
    : IMovieSessionSeatRepository
{
    public async Task AddAsync(MovieSessionSeat movieSessionSeat, CancellationToken cancellationToken)
    {
        context.MovieSessionSeats.Add(movieSessionSeat);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(ICollection<MovieSessionSeat> movieSessionSeats,
        CancellationToken cancellationToken)
    {
        try
        {
            context.MovieSessionSeats.UpdateRange(movieSessionSeats);
            await context.SaveChangesAsync(cancellationToken);

            foreach (var movieSessionSeat in movieSessionSeats)
            {
                await domainEventTracker.PublishDomainEvents(movieSessionSeat, cancellationToken);
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to update MovieSessionSeats");
            throw;
        }
    }

    public async Task UpdateAsync(MovieSessionSeat movieSessionSeat, CancellationToken cancellationToken)
    {
        try
        {
            context.MovieSessionSeats.Update(movieSessionSeat);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to update MovieSessionSeats");
            throw;
        }


        await domainEventTracker.PublishDomainEvents(movieSessionSeat, cancellationToken);
    }

    public async Task<ICollection<MovieSessionSeat>> GetByMovieSessionIdAsync(Guid movieSessionId,
        CancellationToken cancellationToken)
    {
        return await context.MovieSessionSeats
            .Where(t => t.MovieSessionId == movieSessionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<MovieSessionSeat?> GetByIdAsync(Guid movieSessionId, short seatRow, short seatNumber,
        CancellationToken cancellationToken)
    {
        return await context
            .MovieSessionSeats
            .FirstOrDefaultAsync(t => t.MovieSessionId == movieSessionId &&
                                      t.SeatRow == seatRow &&
                                      t.SeatNumber == seatNumber,
                cancellationToken);
    }
}