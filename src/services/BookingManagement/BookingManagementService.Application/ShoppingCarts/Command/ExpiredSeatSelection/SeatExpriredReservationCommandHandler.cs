using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.PriceServices;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ExpiredSeatSelection;

public record SeatExpiredSelectionCommand
    (Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingKartId) : INotification;

public class SeatExpiredReservationEventHandler : INotificationHandler<SeatExpiredSelectionCommand>
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

        var cart = await _shoppingCartRepository.GetByIdAsync(movieSessionSeat.ShoppingCartId);

        if (cart is null)
        {
            _logger.Warning( "Couldnot find ShoppingCart. " +
                             " movieSessionSeat:{@movieSessionSeat}, request:{@request}",
                movieSessionSeat,
                request);
            return;
        }


        var removeResult = cart.TryRemoveSeats(request.SeatRow, request.SeatNumber);

        if (removeResult)
        {
            cart.CalculateCartAmount(new PriceService());
            await _shoppingCartRepository.SetAsync(cart);
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
        
       // await _shoppingCartNotifier.SentShoppingCartState(cart);
    }
}