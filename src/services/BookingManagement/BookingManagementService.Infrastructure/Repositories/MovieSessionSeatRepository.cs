using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Seats;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Serilog;

namespace CinemaTicketBooking.Infrastructure.Repositories;

public class MovieSessionSeatRepository : IMovieSessionSeatRepository
{
    private readonly CinemaContext _context;
    private readonly ILogger _logger;

    private readonly IDomainEventTracker _domainEventTracker;

    public MovieSessionSeatRepository(CinemaContext context,
        ILogger logger,
        IDomainEventTracker domainEventTracker)
    {
        _context = context;
        _logger = logger;
        _domainEventTracker = domainEventTracker;
    }

    public async Task AddAsync(MovieSessionSeat movieSessionSeat, CancellationToken cancellationToken)
    {
        _context.MovieSessionSeats.Add(movieSessionSeat);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MovieSessionSeat movieSessionSeat, CancellationToken cancellationToken)
    {
        try
        {
            _context.MovieSessionSeats.Update(movieSessionSeat);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error("Update MovieSessionSeatRepository {@e}", e);
        }


        await _domainEventTracker.PublishDomainEvents(movieSessionSeat, cancellationToken);
    }

    private List<(string, string)> CreateWithValues<T>(PropertyValues values)
    {
        var entity = new List<(string, string)>();

        foreach (var name in values.Properties)
        {
            entity.Add(new(name.Name, values[name.Name].ToString()));
        }

        return entity;
    }

    public async Task<ICollection<MovieSessionSeat>> GetByMovieSessionIdAsync(Guid movieSessionId,
        CancellationToken cancellationToken)
    {
        return await _context.MovieSessionSeats.Where(t => t.MovieSessionId == movieSessionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<MovieSessionSeat?> GetByIdAsync(Guid movieSessionId, short seatRow, short seatNumber,
        CancellationToken cancellationToken)
    {
        return await _context
            .MovieSessionSeats
            .FirstOrDefaultAsync(t => t.MovieSessionId == movieSessionId &&
                                      t.SeatRow == seatRow &&
                                      t.SeatNumber == seatNumber,
                cancellationToken);
    }
}