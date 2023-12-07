using CinemaTicketBooking.Domain.Common.Events;

namespace CinemaTicketBooking.Domain.ShoppingCarts.Events;

public abstract record ShoppingCartDomainEvent(
    Guid ShoppingCartId
) : IDomainEvent;



public sealed record SeatAddedToShoppingCartDomainEvent(
    Guid MovieSessionId,
    short SeatRow,
    short SeatNumber,
    Guid ShoppingCartId
) : ShoppingCartDomainEvent(ShoppingCartId);

public sealed record SeatRemovedFromShoppingCartDomainEvent(
    Guid MovieSessionId,
    short SeatRow,
    short SeatNumber,
    Guid ShoppingCartId
) : ShoppingCartDomainEvent(ShoppingCartId);

public sealed record ShoppingCartCreatedDomainEvent(
    Guid ShoppingCartId
) : ShoppingCartDomainEvent(ShoppingCartId);

public sealed record ShoppingCartReservedDomainEvent(
    Guid ShoppingCartId
) : ShoppingCartDomainEvent(ShoppingCartId);

public sealed record ShoppingCartPurchaseDomainEvent(
    Guid ShoppingCartId
) : ShoppingCartDomainEvent(ShoppingCartId);

public sealed record ShoppingCartCleanedDomainEvent(
    Guid ShoppingCartId
) : ShoppingCartDomainEvent(ShoppingCartId);

public sealed record ShoppingCartAssignedToClientDomainEvent(
    Guid ShoppingCartId
) : ShoppingCartDomainEvent(ShoppingCartId);

public sealed record ShoppingCartDeletedDomainEvent(
    ShoppingCart ShoppingCart
) : ShoppingCartDomainEvent(ShoppingCart.Id);