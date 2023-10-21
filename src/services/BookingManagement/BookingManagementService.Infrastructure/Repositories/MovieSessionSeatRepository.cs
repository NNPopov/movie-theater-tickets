using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Seats;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Repositories;

public class MovieSessionSeatRepository : IMovieSessionSeatRepository
{
    private readonly CinemaContext _context;
    private readonly IMediator _mediator;

    public MovieSessionSeatRepository(CinemaContext context, IMediator mediator)
    {
        _context = context;

        _mediator = mediator;
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
        
        await PublishDomainEvents(movieSessionSeat);
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
    


    private async Task PublishDomainEvents(IAggregateRoot shoppingCart, CancellationToken cancellationToken = default)
    {
        var domainEvents = shoppingCart.DomainEvents;

        IEnumerable<Task> tasks = domainEvents.Select(domainEvent =>
        {
            var baseApplicationEventBuilder = typeof(BaseApplicationEvent<>).MakeGenericType(domainEvent.GetType());

            var appEvent = Activator.CreateInstance(baseApplicationEventBuilder,
                domainEvent
            );

            return  _mediator.Publish(appEvent, cancellationToken);
        });

        await Task.WhenAll(tasks);
        
        shoppingCart.ClearDomainEvents();
    }
}