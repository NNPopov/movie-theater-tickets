using CinemaTicketBooking.Domain.Common.Events;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Domain.Common;

public abstract class AggregateRoot : Entity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }
    
    [JsonIgnore]
    protected readonly List<IDomainEvent> _domainEvents = new List<IDomainEvent>();

    /// <summary>
    /// Gets the domain events. This collection is readonly.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Clears all the domain events from the <see cref="AggregateRoot"/>.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    /// <summary>
    /// Adds the specified <see cref="IDomainEvent"/> to the <see cref="AggregateRoot"/>.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}