namespace CinemaTicketBooking.Domain.Seats.Abstractions;

public interface IMovieSessionSeatRepository
{
    Task AddAsync(MovieSessionSeat movieSessionSeat,
        CancellationToken cancellationToken);
    
    Task UpdateAsync(MovieSessionSeat movieSessionSeat,
        CancellationToken cancellationToken);

    Task UpdateRangeAsync(ICollection<MovieSessionSeat> movieSessionSeats,
        CancellationToken cancellationToken);

    Task<ICollection<MovieSessionSeat>> GetByMovieSessionIdAsync(Guid movieSessionId,
        CancellationToken cancellationToken);

    Task<MovieSessionSeat?> GetByIdAsync(Guid movieSessionId, short seatRow, short seatNumber,
        CancellationToken cancellationToken);
}