using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.MovieSessions.Events;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Domain.MovieSessions;

public sealed class MovieSession : AggregateRoot
{
    public Guid MovieId { get; private set; }
    public DateTime SessionDate { get; private set; }
    public Guid CinemaHallId { get; private set; }

    public int TicketsForSale { get; private set; }

    public int SoldTickets { get; private set; }
 //   public ICollection<SeatMovieSession> Seats { get; private set; }

    public bool IsEnabled { get; private set; }

    public bool SalesTerminated =>
        SessionDate >= TimeProvider.System.GetUtcNow().DateTime && TicketsForSale <= SoldTickets;

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
        Guid cinemaHallId,
        DateTime sessionDate,
      //  ICollection<SeatMovieSession> seats,
        bool isEnabled,
        int ticketsForSale) : base(id)
    {
        MovieId = movieId;
        SessionDate = sessionDate;
        CinemaHallId = cinemaHallId;
       // Seats = seats;
        IsEnabled = isEnabled;
        TicketsForSale = ticketsForSale;
        SoldTickets = 0;
    }

    public static MovieSession Create(
        Guid movieId,
        Guid auditoriumId,
        DateTime sessionDate,
       // ICollection<SeatMovieSession> seats,
        int ticketsForSale)
    {
        var movieSession = new MovieSession(
            Guid.NewGuid(),
            movieId,
            auditoriumId,
            sessionDate,
        //    seats,
            false,
            ticketsForSale
        );

        movieSession.AddDomainEvent(new MovieSessionCreatedDomainEvent(movieSession));

        return movieSession;
    }
}