using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.AssingClientCart;

public record AssignClientCartCommand(Guid ShoppingCartId, Guid ClientId) : //IdempotentRequest(RequestId),
    IRequest<Result>;

public class AssignClientCartCommandHandler(IShoppingCartRepository shoppingCartRepository,
        IShoppingCartNotifier shoppingCartNotifier)
    : IRequestHandler<AssignClientCartCommand, Result>
{
    public async Task<Result> Handle(AssignClientCartCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await shoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        if (cart == null)
        {
            return DomainErrors<AssignClientCartCommandHandler>.NotFound("Shopping cart not found");
        }
        
        var existingShoppingCartId = await shoppingCartRepository.GetActiveShoppingCartByClientIdAsync(request.ClientId);

        if (existingShoppingCartId != Guid.Empty)
        {
            var existingShoppingCart = await shoppingCartRepository.GetByIdAsync(existingShoppingCartId);

            if (existingShoppingCart != null && existingShoppingCartId != request.ShoppingCartId)
            {
                return DomainErrors<AssignClientCartCommandHandler>.ConflictException("Active Shopping cart already exists");
            }
        }


        var result = cart.AssignClientId(request.ShoppingCartId);

        if (result.IsFailure)
        {
            return result;
        }

        await shoppingCartRepository.SetAsync(cart);

        await shoppingCartRepository.SetClientActiveShoppingCartAsync(request.ClientId, request.ShoppingCartId);
        
        shoppingCartNotifier.ReassignCartToClientID(cart);

        await shoppingCartNotifier.SentShoppingCartState(cart);

        return Result.Success();
    }
}