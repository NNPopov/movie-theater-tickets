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

    public SeatExpiredReservationEventHandler(
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository,
        ILogger logger)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _logger = logger;
    }

    public async Task Handle(SeatExpiredSelectionEvent request,
        CancellationToken cancellationToken)
    {
        var movieSessionSeat =
            await _movieSessionSeatRepository.GetByIdAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber,
                cancellationToken);

        if (movieSessionSeat is null)
        {
            _logger.Warning("Couldnot find MovieSessionSeat, Id:", request);
            return;
        }

        var cart = await _shoppingCartRepository.TryGetCart(movieSessionSeat.ShoppingCartId);

        if (cart is null)
        {
            _logger.Warning( "Couldnot find ShoppingCart, Id:", movieSessionSeat.ShoppingCartId);
            return;
        }


        var removeResult = cart.TryRemoveSeats(new SeatShoppingCart(request.SeatRow, request.SeatNumber));

        if (removeResult)
        {
            await _shoppingCartRepository.TrySetCart(cart);
        }
    }
}