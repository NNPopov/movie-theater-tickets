using CinemaTicketBooking.Domain.Seats;

namespace CinemaTicketBooking.Application.Abstractions;

public interface IMovieSessionSeatRepository
{
    Task AddAsync(MovieSessionSeat movieSessionSeat,
        CancellationToken cancellationToken);
    
    Task UpdateAsync(MovieSessionSeat movieSessionSeat,
        CancellationToken cancellationToken);

    Task<ICollection<MovieSessionSeat>> GetByMovieSessionIdAsync(Guid movieSessionId,
        CancellationToken cancellationToken);

    Task<MovieSessionSeat?> GetByIdAsync(Guid movieSessionId, short seatRow, short seatNumber,
        CancellationToken cancellationToken);
}