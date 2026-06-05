using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.Common.Behaviours;

/// <summary>
/// Central object-level authorization guard for cart-scoped requests (ADR-003 slice 1).
/// Loads the cart by <see cref="ICartScopedRequest.ShoppingCartId"/> and applies the
/// two-mode rule:
/// <list type="bullet">
/// <item>cart not found ⇒ pass through (the handler keeps its existing 404; existence is
/// not leaked as a 403);</item>
/// <item>anonymous cart (<c>ClientId == Guid.Empty</c>) ⇒ pass through (guest capability
/// model preserved);</item>
/// <item>assigned cart with an authenticated owner ⇒ pass through;</item>
/// <item>assigned cart with a non-owner or unauthenticated caller ⇒ throw
/// <see cref="ForbiddenAccessException"/> (mapped centrally to 403).</item>
/// </list>
/// It signals a breach by throwing so it composes uniformly over the command
/// (<c>Result</c>) and the query (<c>ShoppingCart</c>); it sets no HTTP status and touches
/// no <c>HttpContext</c>.
/// </summary>
public class CartOwnershipBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICartScopedRequest
{
    private readonly IActiveShoppingCartRepository _carts;
    private readonly ICurrentUser _currentUser;

    public CartOwnershipBehaviour(IActiveShoppingCartRepository carts, ICurrentUser currentUser)
    {
        _carts = carts;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cart = await _carts.GetByIdAsync(request.ShoppingCartId);

        // Not found ⇒ let the handler own the 404 (do not leak existence as a 403).
        if (cart is null)
            return await next();

        // Anonymous cart ⇒ pure capability; any caller passes (guest model preserved).
        if (cart.ClientId == Guid.Empty)
            return await next();

        // Assigned cart ⇒ strong ownership: caller must be authenticated AND be the owner.
        if (!_currentUser.IsAuthenticated || _currentUser.ClientId != cart.ClientId)
            throw new ForbiddenAccessException();

        return await next();
    }
}
