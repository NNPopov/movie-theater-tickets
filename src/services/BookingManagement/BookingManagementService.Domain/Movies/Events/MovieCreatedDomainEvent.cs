using CinemaTicketBooking.Domain.Common.Events;

namespace CinemaTicketBooking.Domain.Movies.Events;

public sealed class MovieCreatedDomainEvent:IDomainEvent
{
    internal MovieCreatedDomainEvent(Movie movie) => Movie = movie;

    public Movie Movie { get; }
}