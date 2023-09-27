using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions;

public interface ISeatStateRepository
{
    Task DeleteAsync(Guid movieSessionId, short seatRow, short seatNumber);
    
    Task<ICollection<SeatDto>> GetReservedSeats(Guid showtimeId);

    Task<string> StringGetAsync(string key);

    Task<bool> StringSetIfNotExistsAsync(string key, string value, TimeSpan? expiry);

    Task<bool> SetAsync(SeatSelectedInfo value, TimeSpan? expiry);

    Task<T?> GetAsync<T>(string key);

    Task<SeatSelectedInfo> GetAsync(Guid movieSessionId, short seatRow, short seatNumber);

  
    Task<bool> SetAsync<T>(string key, T value);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry);

    // Task<ShoppingCart> TryGetCart(Guid kartId);
    //
    // Task<ShoppingCart> TrySetCart(ShoppingCart shoppingCart);
}