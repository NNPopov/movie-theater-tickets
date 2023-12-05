using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Common;
using CinemaTicketBooking.Application.ShoppingCarts.Base;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;

public record CreateShoppingCartCommand(short MaxNumberOfSeats, Guid RequestId) :  IdempotentRequest(RequestId),
    IRequest<CreateShoppingCartResponse>;

public record CreateShoppingCartResponse(Guid ShoppingCartId, string HashId);

public class CreateShoppingCartCommandHandler(IActiveShoppingCartRepository activeShoppingCartRepository,
    IShoppingCartLifecycleManager shoppingCartLifecycleManager)
    :ActiveShoppingCartHandler(activeShoppingCartRepository, shoppingCartLifecycleManager),
        IRequestHandler<CreateShoppingCartCommand, CreateShoppingCartResponse>
{
    public async Task<CreateShoppingCartResponse> Handle(CreateShoppingCartCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingCart = ShoppingCart.Create(request.MaxNumberOfSeats);
        await SaveShoppingCart(shoppingCart);
        return new CreateShoppingCartResponse(shoppingCart.Id, shoppingCart.HashId);
    }
}