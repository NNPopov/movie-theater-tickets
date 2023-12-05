using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Application.ShoppingCarts.Base;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.PriceServices;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.UnselectSeats;

public record UnselectSeatCommand(Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId)
    : IRequest<bool>;

public class UnselectSeatCommandHandler : ActiveShoppingCartHandler , IRequestHandler<UnselectSeatCommand, bool>
{
    private readonly IMovieSessionsRepository _movieSessionsRepository;
    
    private readonly IShoppingCartSeatLifecycleManager _shoppingCartSeatLifecycleManager;
    
    private readonly ILogger _logger;

    public UnselectSeatCommandHandler(
        IMovieSessionsRepository movieSessionsRepository,
        IShoppingCartSeatLifecycleManager shoppingCartSeatLifecycleManager,
        IShoppingCartLifecycleManager shoppingCartLifecycleManager,
        IActiveShoppingCartRepository activeShoppingCartRepository,
        ILogger logger):base(activeShoppingCartRepository, shoppingCartLifecycleManager)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _shoppingCartSeatLifecycleManager = shoppingCartSeatLifecycleManager;
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

        var cart = await ActiveShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        if (cart is null)
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));

        if (cart.MovieSessionId != request.MovieSessionId)
        {
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));
        }

        cart.TryRemoveSeats(request.SeatRow, request.SeatNumber);
        
        await SaveShoppingCart(cart);


        //Step 2: Remove teptorary select

        var reservedInRedis =
            await _shoppingCartSeatLifecycleManager.GetAsync(request.MovieSessionId, request.SeatRow,
                request.SeatNumber);

        if (reservedInRedis is not null)
        {
            await _shoppingCartSeatLifecycleManager.DeleteAsync(request.MovieSessionId, request.SeatRow,
                request.SeatNumber);
        }


        //Step 3: return seat back to store 


        _logger.Information("Cart was updated {@Cart}", cart);
        return true;
    }
}