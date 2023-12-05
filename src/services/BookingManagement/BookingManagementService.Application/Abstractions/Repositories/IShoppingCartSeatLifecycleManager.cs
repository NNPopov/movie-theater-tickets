using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions.Repositories;

public interface IShoppingCartSeatLifecycleManager
{
    Task DeleteAsync(Guid movieSessionId, short seatRow, short seatNumber);

    Task<SeatShoppingCart> GetAsync(Guid movieSessionId, short seatRow, short seatNumber);

    Task<bool> SetAsync(Guid movieSessionId, SeatShoppingCart seatShoppingCart);
}