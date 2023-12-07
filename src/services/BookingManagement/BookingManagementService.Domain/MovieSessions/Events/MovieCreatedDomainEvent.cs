using CinemaTicketBooking.Domain.Common.Events;

namespace CinemaTicketBooking.Domain.MovieSessions.Events;

public sealed class MovieSessionCreatedDomainEvent:IDomainEvent
{
    internal MovieSessionCreatedDomainEvent(MovieSession movieSession) => MovieSession = movieSession;

    public MovieSession MovieSession { get; }
}