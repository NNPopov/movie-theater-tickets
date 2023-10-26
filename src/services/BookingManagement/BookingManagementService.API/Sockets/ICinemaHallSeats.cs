using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;

namespace CinemaTicketBooking.Api.Sockets;

public interface ICinemaHallSeats
{
    Task SentState(ICollection<MovieSessionSeatDto> seats);
    
    Task SentShoppingCartState(ShoppingCartDto shoppingCart);
}