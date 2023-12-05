using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions.Repositories;

public interface IShoppingCartLifecycleManager
{
    Task DeleteAsync(Guid shoppingCartId);

    Task<SeatShoppingCart> GetAsync(Guid shoppingCartId);

    Task SetAsync(Guid shoppingCartId);
}