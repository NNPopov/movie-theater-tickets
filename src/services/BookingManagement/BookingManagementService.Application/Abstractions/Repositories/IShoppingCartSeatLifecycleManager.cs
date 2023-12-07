using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions.Repositories;

public interface IShoppingCartSeatLifecycleManager
{
    Task DeleteAsync(Guid movieSessionId, short seatRow, short seatNumber);

    Task<bool> IsSeatReservedAsync(Guid movieSessionId, short seatRow, short seatNumber);

    Task<bool> SetAsync(Guid movieSessionId, SeatShoppingCart seatShoppingCart);
    Task<bool> SetAsync(Guid movieSessionId, Guid shoppingCartId, short seatRow, short seatNumber, DateTime expires);
}