using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Events.SeatStatusUpdated;

public class
    ShoppingCartUpdatedEventHandler<T>(
        IShoppingCartNotifier shoppingCartNotifier,
        ILogger logger,
        IShoppingCartRepository shoppingCartRepository)
    : INotificationHandler<BaseApplicationEvent<T>>
    where T : ShoppingCartDomainEvent
{
    public async Task Handle(BaseApplicationEvent<T> request,
        CancellationToken cancellationToken)
    {
        var eventBody = (ShoppingCartDomainEvent)request.Event;


        var shoppingCart = await shoppingCartRepository.GetByIdAsync(eventBody.ShoppingCartId);

        if (shoppingCart is null)
        {
            logger.Debug("ShoppingCart was not find {@ShoppingCartId}", eventBody.ShoppingCartId);
            return;
        }

        await shoppingCartNotifier.SentShoppingCartState(shoppingCart);

        logger.Debug("ShoppingCart state saw send {@ShoppingCart}", shoppingCart);
    }
}