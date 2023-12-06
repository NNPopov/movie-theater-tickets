using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Events;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Events.SeatStatusUpdated;

public class
    ShoppingCartDeletedEventHandler(
        IShoppingCartNotifier shoppingCartNotifier,
        ILogger logger)
    : INotificationHandler<BaseApplicationEvent<ShoppingCartDeletedDomainEvent>>
{
    public async Task Handle(BaseApplicationEvent<ShoppingCartDeletedDomainEvent> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var eventBody = request.Event as ShoppingCartDeletedDomainEvent;
            if (eventBody == null)
            {
                logger.Error("Unable to cast event to {@ShoppingCartDeletedDomainEvent}", request);
                return;
            }

            await shoppingCartNotifier.SentShoppingCartState(eventBody.ShoppingCart);

            logger.Debug("ShoppingCartDomainEvent state {@ShoppingCart} delete", eventBody.ShoppingCart);
            
        }
        catch (Exception e)
        {
            logger.Error(e,
                "Error SentShoppingCartState:{@ShoppingCartDeletedDomainEvent}",
                request);
        }
    }
}