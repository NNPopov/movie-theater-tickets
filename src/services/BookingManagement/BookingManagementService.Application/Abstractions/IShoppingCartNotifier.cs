using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions;

public interface IShoppingCartNotifier
{
    Task SentShoppingCartState(ShoppingCart shoppingCart);
    
    void ReassignCartToClientID(ShoppingCart shoppingCart);
}