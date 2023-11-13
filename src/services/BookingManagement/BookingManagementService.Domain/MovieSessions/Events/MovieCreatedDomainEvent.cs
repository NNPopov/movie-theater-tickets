using CinemaTicketBooking.Domain.Common.Events;

namespace CinemaTicketBooking.Domain.MovieSessions.Events;

public sealed class ShowtimeCreatedDomainEvent:IDomainEvent
{
    internal ShowtimeCreatedDomainEvent(MovieSession movieSession) => MovieSession = movieSession;

    public MovieSession MovieSession { get; }
}