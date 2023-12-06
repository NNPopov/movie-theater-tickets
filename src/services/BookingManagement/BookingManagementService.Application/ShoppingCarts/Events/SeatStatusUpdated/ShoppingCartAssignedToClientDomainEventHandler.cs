using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts.Events;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Events.SeatStatusUpdated;

public class
    ShoppingCartAssignedToClientDomainEventHandler(
        IShoppingCartNotifier shoppingCartNotifier,
        ILogger logger,
        IActiveShoppingCartRepository activeShoppingCartRepository)
    : INotificationHandler<
        BaseApplicationEvent<ShoppingCartAssignedToClientDomainEvent>>
{
    public async Task Handle(BaseApplicationEvent<ShoppingCartAssignedToClientDomainEvent> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var eventBody = request.Event as ShoppingCartAssignedToClientDomainEvent;

            if (eventBody == null)
            {
                logger.Error("Unable to cast event to {@ShoppingCartAssignedToClientDomainEvent}", request);
                return;
            }


            var shoppingCart = await activeShoppingCartRepository.GetByIdAsync(eventBody.ShoppingCartId);
            if (shoppingCart is null)
            {
                logger.Warning("ShoppingCart was not find {@ShoppingCartId}", eventBody.ShoppingCartId);
                return;
            }

            shoppingCartNotifier.ReassignCartToClientId(shoppingCart);

            await shoppingCartNotifier.SentShoppingCartState(shoppingCart);

            logger.Debug("ShoppingCartAssignedToClientDomainEvent state saw send {@ShoppingCart}", shoppingCart);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error  SentShoppingCartState and send ReassignCartToClient:{@ShoppingCartAssignedToClientDomainEvent}", request);
        }
    }
}