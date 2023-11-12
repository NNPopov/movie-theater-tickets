using CinemaTicketBooking.Domain.Common.Events;

namespace CinemaTicketBooking.Domain.CinemaHalls.Events;

public sealed class AuditoriumCreatedDomainEvent:IDomainEvent
{
    internal AuditoriumCreatedDomainEvent(CinemaHall cinemaHall) => CinemaHall = cinemaHall;

    public CinemaHall CinemaHall { get; }
}