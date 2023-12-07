using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts.Events;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Events.SeatStatusUpdated;

internal sealed class
    ShoppingCartUpdatedEventHandler<T>(
        IShoppingCartNotifier shoppingCartNotifier,
        ILogger logger,
        IActiveShoppingCartRepository activeShoppingCartRepository)
    : INotificationHandler<BaseApplicationEvent<T>>
    where T : ShoppingCartDomainEvent
{
    public async Task Handle(BaseApplicationEvent<T> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var eventBody = request.Event as ShoppingCartDomainEvent;
            if (eventBody == null)
            {
                logger.Error("Unable to cast event to {@ShoppingCartDomainEvent}", request);
                return;
            }

            var shoppingCart = await activeShoppingCartRepository.GetByIdAsync(eventBody.ShoppingCartId);

            if (shoppingCart is null)
            {
                logger.Debug("ShoppingCart was not find {@ShoppingCartId}", eventBody.ShoppingCartId);
                return;
            }

            await shoppingCartNotifier.SentShoppingCartState(shoppingCart);

            logger.Debug("ShoppingCart state saw send {@ShoppingCart}", shoppingCart);
        }
        catch (Exception e)
        {
            logger.Error(e,
                "Error SentShoppingCartState:{@ShoppingCartDeletedDomainEvent}",
                request);
        }
    }
}