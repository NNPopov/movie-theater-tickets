using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions;

public interface IShoppingCartRepository
{
    Task<ShoppingCart> TrySetCart(ShoppingCart shoppingCart);

    Task<ShoppingCart> TryGetCart(Guid cartId);

}