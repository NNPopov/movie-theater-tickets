using AutoMapper;
using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Microsoft.AspNetCore.SignalR;

namespace CinemaTicketBooking.Api.Sockets;

public class ShoppingCartNotifier(IHubContext<CinemaHallSeatsHub, IBookingManagementStateUpdater> context,
    IConnectionManager connectionManager, 
    IMapper mapper,
    Serilog.ILogger logger):IShoppingCartNotifier
{
    public async Task SentShoppingCartState(ShoppingCart shoppingCart)
    {
        try
        {
           var connections = connectionManager.GetConnectionId(shoppingCart.Id);

           var shoppingCartDto = mapper.Map<ShoppingCartDto>(shoppingCart);
           foreach (var connection in connections)
           {
               await context.Clients.Client(connection).SentShoppingCartState(shoppingCartDto);
           }
            
           logger.Debug("Updates have been sent to subscribers of shoppingCartId:{@ShoppingCartId}",
               shoppingCart.Id );
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to sent ShoppingCartState");
        }
    }
}