namespace CinemaTicketBooking.Application.Common.Behaviours;

/// <summary>
/// Marker for a request that operates on a single shopping cart. Implementing it opts the
/// request into <see cref="CartOwnershipBehaviour{TRequest,TResponse}"/> object-level
/// authorization. The existing positional <c>ShoppingCartId</c> on a command/query already
/// satisfies the property, so opting in is a contract-neutral change.
/// </summary>
public interface ICartScopedRequest
{
    Guid ShoppingCartId { get; }
}
