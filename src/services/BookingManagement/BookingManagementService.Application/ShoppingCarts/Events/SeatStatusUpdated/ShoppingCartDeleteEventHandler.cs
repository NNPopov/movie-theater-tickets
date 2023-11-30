using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Events.SeatStatusUpdated;

public class
    ShoppingCartDeleteEventHandler(
        IShoppingCartNotifier shoppingCartNotifier,
        ILogger logger)
    : INotificationHandler<BaseApplicationEvent<ShoppingCartDeletedDomainEvent>>
{
    public async Task Handle(BaseApplicationEvent<ShoppingCartDeletedDomainEvent> request,
        CancellationToken cancellationToken)
    {
        var eventBody = (ShoppingCartDeletedDomainEvent)request.Event;

        await shoppingCartNotifier.SentShoppingCartState(eventBody.ShoppingCart);

        logger.Debug("ShoppingCartDomainEvent state {@ShoppingCart} delete", eventBody.ShoppingCart);
    }
}