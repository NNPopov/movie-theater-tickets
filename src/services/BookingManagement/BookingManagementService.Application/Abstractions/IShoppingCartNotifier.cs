using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.Abstractions;

public interface IShoppingCartNotifier
{
    Task SendShoppingCartState(ShoppingCart shoppingCart);
}