using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions;

public interface IShoppingCartNotifier
{
    Task SentShoppingCartExpiredState(ShoppingCart shoppingCart);
    
    Task SentShoppingCartState(ShoppingCart shoppingCart);
    
    void ReassignCartToClientID(ShoppingCart shoppingCart);
}