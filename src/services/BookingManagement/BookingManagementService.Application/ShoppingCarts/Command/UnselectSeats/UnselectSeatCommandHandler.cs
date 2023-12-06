using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Application.ShoppingCarts.Base;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.UnselectSeats;

public record UnselectSeatCommand(Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId)
    : IRequest<bool>;

internal sealed class UnselectSeatCommandHandler : ActiveShoppingCartHandler, IRequestHandler<UnselectSeatCommand, bool>
{
    private readonly IMovieSessionsRepository _movieSessionsRepository;

    private readonly IShoppingCartSeatLifecycleManager _shoppingCartSeatLifecycleManager;

    public UnselectSeatCommandHandler(
        IMovieSessionsRepository movieSessionsRepository,
        IShoppingCartSeatLifecycleManager shoppingCartSeatLifecycleManager,
        IShoppingCartLifecycleManager shoppingCartLifecycleManager,
        IActiveShoppingCartRepository activeShoppingCartRepository,
        ILogger logger) : base(activeShoppingCartRepository, shoppingCartLifecycleManager, logger)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _shoppingCartSeatLifecycleManager = shoppingCartSeatLifecycleManager;
    }

    public async Task<bool> Handle(UnselectSeatCommand request,
        CancellationToken cancellationToken)
    {
        await EnsureMovieSessionExist(request, cancellationToken);

        var shoppingCart = await GetShoppingCartOrThrow(request);

        EnsureMatchingMovieSession(request, shoppingCart);

        shoppingCart.TryRemoveSeats(request.SeatRow, request.SeatNumber);

        await SaveShoppingCart(shoppingCart);

        await DeleteShoppingCartSeatLifeTime(request);

        Logger.Information("Cart was updated {@Cart}", shoppingCart);
        return true;
    }

    private void EnsureMatchingMovieSession(UnselectSeatCommand request, ShoppingCart? shoppingCart)
    {
        if (shoppingCart.MovieSessionId != request.MovieSessionId)
        {
            Logger.Error("Stored {@MovieSessionId} does not match requested {MovieSessionId}",
                shoppingCart.MovieSessionId, request.MovieSessionId);
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));
        }
    }

    private async Task DeleteShoppingCartSeatLifeTime(UnselectSeatCommand request)
    {
        var reservedInRedis =
            await _shoppingCartSeatLifecycleManager.IsSeatReservedAsync(request.MovieSessionId, request.SeatRow,
                request.SeatNumber);

        if (!reservedInRedis)
        {
            Logger.Information("ShoppingCartSeatLifeTime was not found in system {@UnselectSeatCommand}", request);
        }

        await _shoppingCartSeatLifecycleManager.DeleteAsync(request.MovieSessionId, request.SeatRow,
            request.SeatNumber);
    }

    private async Task EnsureMovieSessionExist(UnselectSeatCommand request, CancellationToken cancellationToken)
    {
        var movieSession = await _movieSessionsRepository
            .GetByIdAsync(
                request.MovieSessionId, cancellationToken);

        if (movieSession is null)
        {
            throw new ContentNotFoundException(request.MovieSessionId.ToString(), nameof(MovieSession));
        }
    }

    private async Task<ShoppingCart> GetShoppingCartOrThrow(UnselectSeatCommand request)
    {
        return await ActiveShoppingCartRepository.GetByIdAsync(request.ShoppingCartId) ??
               throw new ContentNotFoundException(nameof(ShoppingCart), request.ShoppingCartId.ToString());
    }
}