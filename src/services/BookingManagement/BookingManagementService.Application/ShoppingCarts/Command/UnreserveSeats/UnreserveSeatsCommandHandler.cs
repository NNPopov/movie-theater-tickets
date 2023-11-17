using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Common;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.UnreserveSeats;
public record UnreserveSeatsCommand(Guid ShoppingCartId, Guid RequestId) : IdempotentRequest(RequestId), IRequest;

public class UnreserveSeatsCommandHandler : IRequestHandler<UnreserveSeatsCommand>
{
    private ISeatStateRepository _seatStateRepository;


    private readonly IShoppingCartRepository _shoppingCartRepository;

    public UnreserveSeatsCommandHandler(
        IShoppingCartRepository shoppingCartRepository,
        ISeatStateRepository seatStateRepository)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _seatStateRepository = seatStateRepository;
    }

    public async Task Handle(UnreserveSeatsCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await _shoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        cart.ClearCart();
        
        foreach (var seat in cart.Seats)
        {
            await _seatStateRepository.DeleteAsync(cart.MovieSessionId, seat.SeatRow, seat.SeatNumber);
        }
        await _shoppingCartRepository.SetAsync(cart);
    }
}