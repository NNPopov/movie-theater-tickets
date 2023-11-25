using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ShoppingCartExpired;

public record ShoppingCartExpiredCommand
    (Guid ShoppingCartId) : INotification;

public class ShoppingCartExpiredCommandHandler : INotificationHandler<ShoppingCartExpiredCommand>
{

    private readonly ILogger _logger;

    private readonly IShoppingCartRepository _shoppingCartRepository;
    
    private readonly IShoppingCartNotifier _shoppingCartNotifier;

    public ShoppingCartExpiredCommandHandler(
        IShoppingCartRepository shoppingCartRepository,
        ILogger logger,
        IShoppingCartNotifier shoppingCartNotifier)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _logger = logger;
        _shoppingCartNotifier = shoppingCartNotifier;
    }

    public async Task Handle(ShoppingCartExpiredCommand request,
        CancellationToken cancellationToken)
    {

        var cart = await _shoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        if (cart is null)
        {
            _logger.Warning( "Couldnot find ShoppingCartId:{@ShoppingCartId}",
                request.ShoppingCartId);
            return;
        }

        cart.Delete();
        await _shoppingCartRepository.DeleteAsync(cart);
        
        _logger.Warning( "ShoppingCart Deleted:{@ShoppingCart}", cart);

        
        //await _shoppingCartNotifier.SentShoppingCartState(cart);
    }
}