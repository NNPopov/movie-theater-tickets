using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using Microsoft.AspNetCore.SignalR;

namespace CinemaTicketBooking.Api.Sockets;

public class CinemaHallSeatsHub(IConnectionManager connectionManager, 
    Serilog.ILogger logger) : Hub<ICinemaHallSeats>
{
    public async Task JoinGroup(Guid movieSession)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, movieSession.ToString());
        }
        catch (Exception e)
        {
            logger.Error("CinemaHallSeatsHub, JoinGroup: {@e}", e);
        }
    }


    public override Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            var connectionId = Context.ConnectionId;

            connectionManager.RemoveByConnectionId(connectionId);
            
            logger.Warning("CinemaHallSeatsHub, OnDisconnectedAsync: {@exception}", exception);
        }
        catch (Exception e)
        {
            logger.Error("CinemaHallSeatsHub, OnDisconnectedAsync: {@e}", e);
        }

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
            logger.Error("CinemaHallSeatsHub, SendCinemaHallSeatsState: {@e}", e);
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
            logger.Error("CinemaHallSeatsHub, RegisterShoppingCart: {@e}", e);
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
            logger.Error("CinemaHallSeatsHub, SentShoppingCartState: {@e}", e);
        }
    }
}