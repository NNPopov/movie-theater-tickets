using CinemaTicketBooking.Domain.Common;

namespace CinemaTicketBooking.Application.Abstractions;

public interface IDomainEventTracker
{
    Task PublishDomainEvents( IAggregateRoot  aggregateRoot,CancellationToken cancellationToken = default);
}