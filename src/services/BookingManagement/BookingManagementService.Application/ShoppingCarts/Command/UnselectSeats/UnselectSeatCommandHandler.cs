using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.UnselectSeats;

public record UnselectSeatCommand
    (Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId) : IRequest<bool>;

public class UnselectSeatCommandHandler : IRequestHandler<UnselectSeatCommand, bool>
{
    private IMovieSessionsRepository _movieSessionsRepository;
    private IShoppingCartRepository _shoppingCartRepository;

    private ISeatStateRepository _seatStateRepository;

    private readonly IPublisher _publisher;

    private readonly IShoppingCartNotifier _shoppingCartNotifier;

    private readonly ILogger _logger;

    public UnselectSeatCommandHandler(
        IMovieSessionsRepository movieSessionsRepository,
        ISeatStateRepository seatStateRepository,
        IShoppingCartRepository shoppingCartRepository,
        IPublisher publisher, 
        IShoppingCartNotifier shoppingCartNotifier, 
        ILogger logger)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _seatStateRepository = seatStateRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _publisher = publisher;
        _shoppingCartNotifier = shoppingCartNotifier;
        _logger = logger;
    }

    public async Task<bool> Handle(UnselectSeatCommand request,
        CancellationToken cancellationToken)
    {
        var movieSession = await _movieSessionsRepository
            .GetByIdAsync(
                request.MovieSessionId, cancellationToken);

        if (movieSession is null)
            throw new ContentNotFoundException(request.MovieSessionId.ToString(), nameof(MovieSession));

        //Step 1: Remove seat from cart

        var cart = await _shoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        if (cart is null)
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));

        if (cart.MovieSessionId != request.MovieSessionId)
        {
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));
        }

        cart.TryRemoveSeats(new SeatShoppingCart(request.SeatRow, request.SeatNumber));


        await _shoppingCartRepository.SetAsync(cart);

        //Step 2: Remove teptorary select

        var reservedInRedis =
            await _seatStateRepository.GetAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber);

        if (reservedInRedis is not null)
        {
            await _seatStateRepository.DeleteAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber);
        }


        await _shoppingCartNotifier.SentShoppingCartState(cart);

        //Step 3: return seat back to store 


        _logger.Information("Cart was updated {@Cart}", cart);
        return true;
    }
}