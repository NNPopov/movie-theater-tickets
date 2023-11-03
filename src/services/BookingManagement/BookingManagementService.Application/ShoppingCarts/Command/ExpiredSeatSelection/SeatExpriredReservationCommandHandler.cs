using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ExpiredSeatSelection;

public record SeatExpiredSelectionEvent
    (Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingKartId) : INotification;

public class SeatExpiredReservationEventHandler : INotificationHandler<SeatExpiredSelectionEvent>
{
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly ILogger _logger;

    private readonly IShoppingCartRepository _shoppingCartRepository;
    
    private readonly IShoppingCartNotifier _shoppingCartNotifier;

    public SeatExpiredReservationEventHandler(
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository,
        ILogger logger,
        IShoppingCartNotifier shoppingCartNotifier)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _logger = logger;
        _shoppingCartNotifier = shoppingCartNotifier;
    }

    public async Task Handle(SeatExpiredSelectionEvent request,
        CancellationToken cancellationToken)
    {
        var movieSessionSeat =
            await _movieSessionSeatRepository.GetByIdAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber,
                cancellationToken);

        if (movieSessionSeat is null)
        {
            _logger.Warning("Couldnot find MovieSessionSeat, MovieSessionId:{@MovieSessionId)}, SeatRow:{@SeatRow}, SeatNumber:{@SeatNumber} ",
                request.MovieSessionId,
                request.SeatRow,
                request.SeatNumber);
            return;
        }

        var cart = await _shoppingCartRepository.TryGetCart(movieSessionSeat.ShoppingCartId);

        if (cart is null)
        {
            _logger.Warning( "Couldnot find ShoppingCart. " +
                             " movieSessionSeat:{@movieSessionSeat}, request:{@request}",
                movieSessionSeat,
                request);
            return;
        }


        var removeResult = cart.TryRemoveSeats(new SeatShoppingCart(request.SeatRow, request.SeatNumber));

        if (removeResult)
        {
            await _shoppingCartRepository.TrySetCart(cart);
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
        
        await _shoppingCartNotifier.SendShoppingCartState(cart);
    }
}