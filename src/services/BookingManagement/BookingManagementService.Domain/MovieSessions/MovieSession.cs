using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.MovieSessions.Events;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Domain.MovieSessions;

public class MovieSession : AggregateRoot
{
    public Guid MovieId { get; private set; }
    public DateTime SessionDate { get; private set; }
    public Guid AuditoriumId { get; private set; }

    public int TicketsForSale { get; private set; }

    public int SoldTickets { get; private set; }
    public ICollection<SeatMovieSession> Seats { get; private set; }

    public bool IsEnabled { get; private set; }

    public bool SalesTerminated =>
        SessionDate >= TimeProvider.System.GetUtcNow().DateTime && TicketsForSale > SoldTickets;

    public void SetSoldTickets(int soldTickets)
    {
        Ensure.NotEmpty(soldTickets, "The soldTickets is required.", nameof(soldTickets));
        
        SoldTickets = soldTickets;
    }

    private MovieSession()
    {
    }

    private MovieSession(Guid id,
        Guid movieId,
        Guid auditoriumId,
        DateTime sessionDate,
        ICollection<SeatMovieSession> seats,
        bool isEnabled,
        int ticketsForSale) : base(id)
    {
        MovieId = movieId;
        SessionDate = sessionDate;
        AuditoriumId = auditoriumId;
        Seats = seats;
        IsEnabled = isEnabled;
        SoldTickets = 0;
    }

    public static MovieSession Create(
        Guid movieId,
        Guid auditoriumId,
        DateTime sessionDate,
        ICollection<SeatMovieSession> seats,
        int ticketsForSale)
    {
        var showtime = new MovieSession(
            Guid.NewGuid(),
            movieId,
            auditoriumId,
            sessionDate,
            seats,
            false,
            ticketsForSale
        );

        showtime.AddDomainEvent(new ShowtimeCreatedDomainEvent(showtime));

        return showtime;
    }
}

[method: JsonConstructor]
public class SeatMovieSession(short seatRow, short seatNumber) : Seat(seatRow, seatNumber);

public abstract class Seat : ValueObject
{
    protected Seat(short seatRow, short seatNumber)
    {
        SeatRow = seatRow;
        SeatNumber = seatNumber;
    }

    public short SeatRow { get; private set; }
    public short SeatNumber { get; private set; }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return SeatNumber;

        yield return SeatRow;
    }
}