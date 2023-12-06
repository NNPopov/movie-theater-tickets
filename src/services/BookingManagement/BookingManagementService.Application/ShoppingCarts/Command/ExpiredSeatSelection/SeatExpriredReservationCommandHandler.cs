using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.ShoppingCarts.Base;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.PriceServices;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ExpiredSeatSelection;

public record SeatExpiredSelectionCommand
    (Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingKartId) : INotification;

public class SeatExpiredReservationEventHandler :ActiveShoppingCartHandler, INotificationHandler<SeatExpiredSelectionCommand>
{
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;

    public SeatExpiredReservationEventHandler(
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IActiveShoppingCartRepository activeShoppingCartRepository,
        ILogger logger,
        IShoppingCartLifecycleManager shoppingCartLifecycleManager):base(activeShoppingCartRepository, shoppingCartLifecycleManager, logger)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
    }

    public async Task Handle(SeatExpiredSelectionCommand request,
        CancellationToken cancellationToken)
    {
        var movieSessionSeat =
            await _movieSessionSeatRepository.GetByIdAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber,
                cancellationToken);

        if (movieSessionSeat is null)
        {
            Logger.Warning("Couldn't find MovieSessionSeat, MovieSession:{@MovieSession)}",
                request);
            return;
        }

        var cart = await ActiveShoppingCartRepository.GetByIdAsync(movieSessionSeat.ShoppingCartId);

        if (cart is null)
        {
            Logger.Warning( "Couldn't find ShoppingCart. " +
                            " movieSessionSeat:{@MovieSessionSeat}, request:{@Request}",
                movieSessionSeat,
                request);
            return;
        }

        var removeResult = cart.TryRemoveSeats(request.SeatRow, request.SeatNumber);

        if (removeResult)
        {
            await SaveShoppingCart(cart);
        }
        else
        {
            Logger.Warning( "Seat could not be removed from the cart ShoppingCart, Id:{@ShoppingCartId}. " +
                            " MovieSessionId:{@MovieSessionId}, SeatRow:{@SeatRow}, SeatNumber:{@SeatNumber}",
                movieSessionSeat.ShoppingCartId,
                request.MovieSessionId,
                request.SeatRow,
                request.SeatNumber);
        }
        
    }
}