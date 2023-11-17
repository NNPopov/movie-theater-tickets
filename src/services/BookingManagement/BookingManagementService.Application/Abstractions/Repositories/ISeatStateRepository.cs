using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions.Repositories;

public interface ISeatStateRepository
{
    Task DeleteAsync(Guid movieSessionId, short seatRow, short seatNumber);
    
    Task<bool> SetAsync(Guid movieSessionId, short seatRow, short seatNumber, TimeSpan? expiry);
    

    Task<SeatShoppingCart> GetAsync(Guid movieSessionId, short seatRow, short seatNumber);
}