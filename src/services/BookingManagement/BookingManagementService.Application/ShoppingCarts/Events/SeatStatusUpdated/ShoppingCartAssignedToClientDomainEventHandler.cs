using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Events.SeatStatusUpdated;

public class
    ShoppingCartAssignedToClientDomainEventHandler(
        IShoppingCartNotifier shoppingCartNotifier,
        ILogger logger,
        IShoppingCartRepository shoppingCartRepository)
    : INotificationHandler<
        BaseApplicationEvent<ShoppingCartAssignedToClientDomainEvent>>
{
    public async Task Handle(BaseApplicationEvent<ShoppingCartAssignedToClientDomainEvent> request,
        CancellationToken cancellationToken)
    {
        var eventBody = (ShoppingCartAssignedToClientDomainEvent)request.Event;

        var shoppingCart = await shoppingCartRepository.GetByIdAsync(eventBody.ShoppingCartId);
        if (shoppingCart is null)
        {
            logger.Debug("ShoppingCart was not find {@ShoppingCartId}", eventBody.ShoppingCartId);
            return;
        }

        shoppingCartNotifier.ReassignCartToClientId(shoppingCart);

        await shoppingCartNotifier.SentShoppingCartState(shoppingCart);

        logger.Debug("ShoppingCartAssignedToClientDomainEvent state saw send {@ShoppingCart}", shoppingCart);
    }
}