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
    private readonly ILogger _logger;

    public SeatExpiredReservationEventHandler(
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IActiveShoppingCartRepository activeShoppingCartRepository,
        ILogger logger,
        IShoppingCartLifecycleManager shoppingCartLifecycleManager):base(activeShoppingCartRepository, shoppingCartLifecycleManager)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _logger = logger;
    }

    public async Task Handle(SeatExpiredSelectionCommand request,
        CancellationToken cancellationToken)
    {
        var movieSessionSeat =
            await _movieSessionSeatRepository.GetByIdAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber,
                cancellationToken);

        if (movieSessionSeat is null)
        {
            _logger.Warning("Couldnot find MovieSessionSeat, MovieSession:{@MovieSession)}",
                request);
            return;
        }

        var cart = await ActiveShoppingCartRepository.GetByIdAsync(movieSessionSeat.ShoppingCartId);

        if (cart is null)
        {
            _logger.Warning( "Couldn't find ShoppingCart. " +
                             " movieSessionSeat:{@movieSessionSeat}, request:{@request}",
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
            _logger.Warning( "Seat could not be removed from the cart ShoppingCart, Id:{@ShoppingCartId}. " +
                             " MovieSessionId:{@MovieSessionId}, SeatRow:{@SeatRow}, SeatNumber:{@SeatNumber}",
                movieSessionSeat.ShoppingCartId,
                request.MovieSessionId,
                request.SeatRow,
                request.SeatNumber);
        }
        
    }
}