using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.ShoppingCarts.Base;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.AssingClientCart;

public record AssignClientCartCommand(Guid ShoppingCartId, Guid ClientId) : //IdempotentRequest(RequestId),
    IRequest<Result>;

public class AssignClientCartCommandHandler(IActiveShoppingCartRepository activeShoppingCartRepository,   IShoppingCartLifecycleManager shoppingCartLifecycleManager)
    : ActiveShoppingCartHandler(activeShoppingCartRepository, shoppingCartLifecycleManager), IRequestHandler<AssignClientCartCommand, Result>
{
    public async Task<Result> Handle(AssignClientCartCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await activeShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        if (cart == null)
        {
            return DomainErrors<AssignClientCartCommandHandler>.NotFound("Shopping cart not found");
        }
        
        var existingShoppingCartId = await activeShoppingCartRepository.GetActiveShoppingCartByClientIdAsync(request.ClientId);

        if (existingShoppingCartId != Guid.Empty)
        {
            var existingShoppingCart = await activeShoppingCartRepository.GetByIdAsync(existingShoppingCartId);

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

        await SaveShoppingCart(cart);

        await activeShoppingCartRepository.SetClientActiveShoppingCartAsync(request.ClientId, request.ShoppingCartId);

        return Result.Success();
    }
}