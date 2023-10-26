using AutoMapper;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Microsoft.AspNetCore.SignalR;

namespace CinemaTicketBooking.Api.Sockets;

public class ShoppingCartNotifier(IHubContext<CinemaHallSeatsHub, ICinemaHallSeats> context,
    IConnectionManager connectionManager, 
    IMapper _mapper):IShoppingCartNotifier
{
    public async Task SendShoppingCartState(ShoppingCart shoppingCart)
    {
        try
        {
           var connections = connectionManager.GetConnectionId(shoppingCart.Id);

           var shoppingCartDto = _mapper.Map<ShoppingCartDto>(shoppingCart);
           foreach (var connection in connections)
           {
               await context.Clients.Client(connection).SentShoppingCartState(shoppingCartDto);
           }
            

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            // throw;
        }
    }
}