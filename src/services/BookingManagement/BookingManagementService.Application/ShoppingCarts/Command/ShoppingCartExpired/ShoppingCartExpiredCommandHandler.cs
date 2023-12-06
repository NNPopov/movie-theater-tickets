using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ShoppingCartExpired;

public record ShoppingCartExpiredCommand
    (Guid ShoppingCartId) : INotification;

public class ShoppingCartExpiredCommandHandler : INotificationHandler<ShoppingCartExpiredCommand>
{

    private readonly ILogger _logger;

    private readonly IActiveShoppingCartRepository _activeShoppingCartRepository;
    
    public ShoppingCartExpiredCommandHandler(
        IActiveShoppingCartRepository activeShoppingCartRepository,
        ILogger logger)
    {
        _activeShoppingCartRepository = activeShoppingCartRepository;
        _logger = logger;
    }

    public async Task Handle(ShoppingCartExpiredCommand request,
        CancellationToken cancellationToken)
    {

        var cart = await _activeShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        if (cart is null)
        {
            _logger.Warning( "Couldnot find ShoppingCartId:{@ShoppingCartId}",
                request.ShoppingCartId);
            return;
        }

        cart.Delete();
        await _activeShoppingCartRepository.DeleteAsync(cart);
        
        _logger.Warning( "ShoppingCart Deleted:{@ShoppingCart}", cart);
        
    }
}