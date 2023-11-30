using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;

namespace CinemaTicketBooking.Api.Sockets.Abstractions;

public interface IBookingManagementStateUpdater
{
    Task SentCinemaHallSeatsState(ICollection<MovieSessionSeatDto> seats);
    
    Task SentShoppingCartState(ShoppingCartDto shoppingCart);
    
    Task SentServerState(ServerState serverState);
}