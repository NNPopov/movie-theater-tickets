using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ShoppingCartExpired;

public record ShoppingCartExpiredCommand
    (Guid ShoppingCartId) : INotification;

internal sealed  class ShoppingCartExpiredCommandHandler : INotificationHandler<ShoppingCartExpiredCommand>
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
        var cart = await GetShoppingCartOrThrow(request);
        
        cart.Delete();
        await _activeShoppingCartRepository.DeleteAsync(cart);
        
        _logger.Warning( "ShoppingCart was Expired and Deleted:{@ShoppingCart}", cart);
        
    }
    
    private async Task<ShoppingCart> GetShoppingCartOrThrow(ShoppingCartExpiredCommand request)
    {
        return await _activeShoppingCartRepository.GetByIdAsync(request.ShoppingCartId) ??
               throw new ContentNotFoundException(nameof(ShoppingCart), request.ShoppingCartId.ToString());
    }
}