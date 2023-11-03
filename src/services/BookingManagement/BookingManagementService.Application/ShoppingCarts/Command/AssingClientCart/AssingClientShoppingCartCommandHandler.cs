using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.AssingClientCart;

public record AssignClientCartCommand(Guid ShoppingCartId, Guid ClientId) : //IdempotentRequest(RequestId),
    IRequest<AssignClientCartResponse>;

public class AssignClientCartCommandHandler : IRequestHandler<AssignClientCartCommand, AssignClientCartResponse>
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IShoppingCartNotifier _shoppingCartNotifier;
    public AssignClientCartCommandHandler(IShoppingCartRepository shoppingCartRepository, IShoppingCartNotifier shoppingCartNotifier)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _shoppingCartNotifier = shoppingCartNotifier;
    }

    public async Task<AssignClientCartResponse> Handle(AssignClientCartCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await _shoppingCartRepository.TryGetCart(request.ShoppingCartId);

        if (cart == null)
        {
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));
        }

        cart.AssignClientId(request.ShoppingCartId);
        
        await _shoppingCartRepository.TrySetCart(cart);
        
        await _shoppingCartNotifier.SendShoppingCartState(cart);
        
        return new AssignClientCartResponse(cart.Id);
    }
}