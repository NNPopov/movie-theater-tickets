using AutoMapper;
using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.UnselectSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using CinemaTicketBooking.Domain.ShoppingCarts;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.Sockets;

public class BookingManagementServiceHub(
    IConnectionManager connectionManager,
    ILogger logger,
    IMediator mediator,
    ICacheService cacheService,
    IMapper mapper,
    ICinemaHallSeatsNotifier cinemaHallSeatsNotifier
) : Hub<IBookingManagementStateUpdater>
{
    public async Task SeatSelect(Guid shoppingCartId,
        short row,
        short number,
        Guid showtimeId)
    {
        try
        {
            var shoppingCart = await GetShoppingCart(shoppingCartId);

            await SubscribeToCartUpdatesIfNotSubscribed(shoppingCart);

            var query = new SelectSeatCommand(MovieSessionId: showtimeId,
                SeatRow: row,
                SeatNumber: number,
                ShoppingCartId: shoppingCartId);
            var result = await mediator.Send(query);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed select seat");
        }
    }

    public async Task SeatUnselect(Guid shoppingCartId,
        short row,
        short number,
        Guid showtimeId)
    {
        try
        {
            var shoppingCart = await GetShoppingCart(shoppingCartId);

            await SubscribeToCartUpdatesIfNotSubscribed(shoppingCart);

            var command = new UnselectSeatCommand(MovieSessionId: showtimeId,
                SeatRow: row,
                SeatNumber: number,
                ShoppingCartId: shoppingCartId);
            var result = await mediator.Send(command);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed unselect seat");
        }
    }


    public async Task SubscribeToUpdateSeatsGroup(Guid movieSessionId)
    {
        try
        {
            await SubscribeMovieSessionSeatsUpdateIfNeeded(movieSessionId);

            await cinemaHallSeatsNotifier.SendSeatUpdatesDataToSpecificClient(movieSessionId, Context.ConnectionId);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to add to distribution group");
        }
    }

    private async Task SubscribeMovieSessionSeatsUpdateIfNeeded(Guid movieSessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, movieSessionId.ToString());
        logger.Debug("Client {@ConnectionId} was subscribed to MovieSessionSeats update {@MovieSessionId}",
            Context.ConnectionId,
            movieSessionId);
    }

    public async Task RegisterShoppingCart(Guid shoppingCartId)
    {
        try
        {
            var shoppingCart = await GetShoppingCart(shoppingCartId);

            await SubscribeToCartUpdatesIfNotSubscribed(shoppingCart);

            var shoppingCartDto = mapper.Map<ShoppingCartDto>(shoppingCart);


            await Clients.Client(Context.ConnectionId).SentShoppingCartState(shoppingCartDto);

            logger.Debug("The customer has subscribed to shopping cart updates ShoppingCartId:{@ShoppingCartId}",
                shoppingCartId);
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

            logger.Debug("The customer has unsubscribed to shopping cart updates ShoppingCartId:{@ShoppingCartId}",
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

    private async Task<ShoppingCart> GetShoppingCart(Guid shoppingCartId)
    {
        return await mediator.Send(new GetShoppingCartQuery(shoppingCartId));
    }

    private async Task SubscribeToCartUpdatesIfNotSubscribed(ShoppingCart shoppingCart)
    {
        var shoppingCartIdOrClientId = shoppingCart.ClientId != Guid.Empty ? shoppingCart.ClientId : shoppingCart.Id;

        //Add connection if not exists
        connectionManager.AddConnection(shoppingCartIdOrClientId, Context.ConnectionId);
    }
}