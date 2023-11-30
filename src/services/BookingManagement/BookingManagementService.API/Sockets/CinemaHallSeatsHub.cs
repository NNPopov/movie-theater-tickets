using AutoMapper;
using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.Sockets;

public class CinemaHallSeatsHub(IConnectionManager connectionManager,
    ILogger logger,
    IMediator mediator,
    ICacheService cacheService,
    IMapper mapper
) : Hub<IBookingManagementStateUpdater>
{
    public async Task SubscribeToUpdateSeatsGroup(Guid movieSessionId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, movieSessionId.ToString());
            logger.Debug("Client {@ConnectionId} was subscribed to MovieSessionSeats update {@MovieSessionId}",
                Context.ConnectionId,
                movieSessionId);

            var movieSessionSeatsKey = $"MovieSessionSeats:{movieSessionId}";

            var movieSessionSeatDto = await cacheService.TryGet<ICollection<MovieSessionSeatDto>>(movieSessionSeatsKey);

            if (movieSessionSeatDto is not null)
            {
                await Clients.Client(Context.ConnectionId).SentCinemaHallSeatsState(movieSessionSeatDto);
            }
            else
            {
                var query = new GetMovieSessionSeatsQuery(movieSessionId);
                var seats = await mediator.Send(query);

                await cacheService.Set(movieSessionSeatsKey, seats, new TimeSpan(0, 5, 0));

                await Clients.Client(Context.ConnectionId).SentCinemaHallSeatsState(seats);
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to add to distribution group");
        }
    }

    public async Task RegisterShoppingCart(Guid shoppingCardId)
    {
        try
        {
            var cart = await mediator.Send(new GetShoppingCartQuery(shoppingCardId));

            var shoppingCartIdOrClientId = cart.ClientId != Guid.Empty ? cart.ClientId : cart.Id;

            connectionManager.AddConnection(shoppingCartIdOrClientId, Context.ConnectionId);

            var shoppingCartDto = mapper.Map<ShoppingCartDto>(cart);


            await Clients.Client(Context.ConnectionId).SentShoppingCartState(shoppingCartDto);

            logger.Debug("The customer has subscribed to shopping cart updates shoppingCartId:{@ShoppingCartId}",
                shoppingCardId);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to add AddConnection");
        }
    }
    
    public async Task UnsubscribeShoppingCart(Guid shoppingCardId)
    {
        try
        {
            var cart = await mediator.Send(new GetShoppingCartQuery(shoppingCardId));

            var shoppingCartIdOrClientId = cart.ClientId != Guid.Empty ? cart.ClientId : cart.Id;

            connectionManager.RemoveSubscriptionShoppingCartId(shoppingCartIdOrClientId, Context.ConnectionId);
          
            logger.Debug("The customer has unsubscribed to shopping cart updates shoppingCartId:{@ShoppingCartId}",
                shoppingCardId);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to add AddConnection");
        }
    }


    public override Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            var connectionId = Context.ConnectionId;

            connectionManager.RemoveByConnectionId(connectionId);

            logger.Warning(exception, "Client connectionId:{@ConnectionId} was disconnected", connectionId);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to remove from distribution group");
        }

        return base.OnDisconnectedAsync(exception);
    }
}