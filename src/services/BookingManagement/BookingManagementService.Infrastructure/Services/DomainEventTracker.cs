using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.Common;
using MediatR;
using Serilog;

namespace CinemaTicketBooking.Infrastructure;

public class DomainEventTracker : IDomainEventTracker
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public DomainEventTracker(IMediator mediator, ILogger logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishDomainEvents( IAggregateRoot  aggregateRoot,CancellationToken cancellationToken = default)
    {
        try
        {
            var domainEvents =
                aggregateRoot.GetDomainEvents().ToList();

            aggregateRoot.ClearDomainEvents();

            IEnumerable<Task> tasks = domainEvents.Select(domainEvent =>
            {
                var baseApplicationEventBuilder = typeof(BaseApplicationEvent<>).MakeGenericType(domainEvent.GetType());

                var appEvent = Activator.CreateInstance(baseApplicationEventBuilder,
                    domainEvent
                );

                _logger.Debug("Publish event: {AppEvent}, {@DomainEvent}", domainEvent.GetType().ToString(), domainEvent);
                return _mediator.Publish(appEvent, cancellationToken);
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to Publish DomainEvents");

        }
    }
}