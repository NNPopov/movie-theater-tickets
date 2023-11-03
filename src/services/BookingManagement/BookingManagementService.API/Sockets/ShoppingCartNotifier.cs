using AutoMapper;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Microsoft.AspNetCore.SignalR;

namespace CinemaTicketBooking.Api.Sockets;

public class ShoppingCartNotifier(IHubContext<CinemaHallSeatsHub, ICinemaHallSeats> context,
    IConnectionManager connectionManager, 
    IMapper mapper,
    Serilog.ILogger logger):IShoppingCartNotifier
{
    public async Task SendShoppingCartState(ShoppingCart shoppingCart)
    {
        try
        {
           var connections = connectionManager.GetConnectionId(shoppingCart.Id);

           var shoppingCartDto = mapper.Map<ShoppingCartDto>(shoppingCart);
           foreach (var connection in connections)
           {
               await context.Clients.Client(connection).SentShoppingCartState(shoppingCartDto);
           }
            

        }
        catch (Exception e)
        {
            logger.Error("ShoppingCartNotifier {@e}", e);
            Console.WriteLine(e);
            // throw;
        }
    }
}