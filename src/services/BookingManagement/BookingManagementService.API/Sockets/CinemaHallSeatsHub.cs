using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using Microsoft.AspNetCore.SignalR;

namespace CinemaTicketBooking.Api.Sockets;

public class CinemaHallSeatsHub(IConnectionManager connectionManager) : Hub<ICinemaHallSeats>
{
   
    public async Task JoinGroup(Guid movieSession)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, movieSession.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    
    public override Task OnDisconnectedAsync(Exception exception)
    {
        var connectionId = Context.ConnectionId;
        
        connectionManager.RemoveByConnectionId(connectionId);

        return base.OnDisconnectedAsync(exception);
    }
    
    
    public async Task SendCinemaHallSeatsState(Guid movieSession,
        ICollection<MovieSessionSeatDto> seats)
    {
        try
        {
            await Clients.Group(movieSession.ToString()).SentState(seats);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public async Task RegisterShoppingCart(Guid shoppingCardId)
    {
        try
        {
            connectionManager.AddConnection(shoppingCardId, Context.ConnectionId);
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public async Task SentShoppingCartState(ShoppingCartDto shoppingCart)
    {
        try
        {
            var connections = connectionManager.GetConnectionId(shoppingCart.Id);
            
            foreach (var connection in connections)
            {
                await Clients.Client(connection).SentShoppingCartState(shoppingCart);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}