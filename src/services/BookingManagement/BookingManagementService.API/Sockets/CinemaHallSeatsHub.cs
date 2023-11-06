using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using Microsoft.AspNetCore.SignalR;

namespace CinemaTicketBooking.Api.Sockets;

public class CinemaHallSeatsHub(IConnectionManager connectionManager, 
    Serilog.ILogger logger) : Hub<IBookingManagementStateUpdater>
{
    public async Task SubscribeToUpdateSeatsGroup(Guid movieSessionId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, movieSessionId.ToString());
            logger.Debug( "Client {@ConnectionId} was subscribed to MovieSessionSeats update {@MovieSessionId}",
                Context.ConnectionId, movieSessionId);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to add to distribution group");
        }
    }


    public override Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            var connectionId = Context.ConnectionId;

            connectionManager.RemoveByConnectionId(connectionId);
            
            logger.Warning(exception,"Client connectionId:{@ConnectionId} was disconnected", connectionId );
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to remove from distribution group");
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterShoppingCart(Guid shoppingCardId)
    {
        try
        {
            connectionManager.AddConnection(shoppingCardId, Context.ConnectionId);
            
            logger.Debug("The customer has subscribed to shopping cart updates shoppingCartId:{@ShoppingCartId}",
                shoppingCardId );
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to add AddConnection");
        }
    }
}