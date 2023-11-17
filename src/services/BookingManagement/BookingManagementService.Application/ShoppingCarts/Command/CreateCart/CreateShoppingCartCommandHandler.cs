using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Common;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;

public record CreateShoppingCartCommand(short MaxNumberOfSeats, Guid RequestId) : IdempotentRequest(RequestId),
    IRequest<CreateShoppingCartResponse>;

public record CreateShoppingCartResponse(Guid ShoppingCartId, string HashId);

public class CreateShoppingCartCommandHandler(IShoppingCartRepository shoppingCartRepository,
        IShoppingCartNotifier shoppingCartNotifier)
    : IRequestHandler<CreateShoppingCartCommand, CreateShoppingCartResponse>
{
    public async Task<CreateShoppingCartResponse> Handle(CreateShoppingCartCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingCart = ShoppingCart.Create(request.MaxNumberOfSeats);
        await shoppingCartRepository.SetAsync(shoppingCart);
        
        await shoppingCartNotifier.SentShoppingCartState(shoppingCart);

        return new CreateShoppingCartResponse(shoppingCart.Id, shoppingCart.HashId);
    }
}