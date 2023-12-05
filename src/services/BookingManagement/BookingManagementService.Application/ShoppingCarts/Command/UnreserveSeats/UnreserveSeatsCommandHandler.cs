using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Common;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.UnreserveSeats;
public record UnreserveSeatsCommand(Guid ShoppingCartId, Guid RequestId) : IdempotentRequest(RequestId), IRequest;

public class UnreserveSeatsCommandHandler : IRequestHandler<UnreserveSeatsCommand>
{
    private readonly IShoppingCartSeatLifecycleManager _shoppingCartSeatLifecycleManager;
    private readonly IShoppingCartLifecycleManager _shoppingCartLifecycleManager;
    private readonly IActiveShoppingCartRepository _activeShoppingCartRepository;

    public UnreserveSeatsCommandHandler(
        IActiveShoppingCartRepository activeShoppingCartRepository,
        IShoppingCartSeatLifecycleManager shoppingCartSeatLifecycleManager, 
        IShoppingCartLifecycleManager shoppingCartLifecycleManager)
    {
        _activeShoppingCartRepository = activeShoppingCartRepository;
        _shoppingCartSeatLifecycleManager = shoppingCartSeatLifecycleManager;
        _shoppingCartLifecycleManager = shoppingCartLifecycleManager;
    }

    public async Task Handle(UnreserveSeatsCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await _activeShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        cart.ClearCart();
        
        foreach (var seat in cart.Seats)
        {
            await _shoppingCartSeatLifecycleManager.DeleteAsync(cart.MovieSessionId, seat.SeatRow, seat.SeatNumber);
        }
        await _activeShoppingCartRepository.SaveAsync(cart);
        await _shoppingCartLifecycleManager.SetAsync(cart.Id);
    }
}