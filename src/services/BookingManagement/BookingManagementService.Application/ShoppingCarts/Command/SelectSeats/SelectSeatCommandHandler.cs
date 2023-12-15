using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Application.ShoppingCarts.Base;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;

public record SelectSeatCommand(Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId)
    : IRequest<Result>;

public class SelectSeatCommandHandler(
    IShoppingCartSeatLifecycleManager shoppingCartSeatLifecycleManager,
    IActiveShoppingCartRepository activeShoppingCartRepository,
    IDistributedLock distributedLock,
    ILogger logger,
    MovieSessionSeatService movieSessionSeatService,
    IShoppingCartLifecycleManager shoppingCartLifecycleManager)
    : ActiveShoppingCartHandler(activeShoppingCartRepository, shoppingCartLifecycleManager, logger),
        IRequestHandler<SelectSeatCommand, Result>
{
    public async Task<Result> Handle(SelectSeatCommand request,
        CancellationToken cancellationToken)
    {
        var lockKey = $"lock:{request.MovieSessionId}:{request.SeatRow}:{request.SeatNumber}";


        await using (var lockHandler = await distributedLock.TryAcquireAsync(lockKey,
                         cancellationToken: cancellationToken))
        {
            EnsureDistributedLockIsNotLocked(lockHandler, lockKey);

            ShoppingCart cart = await GetShoppingCartOrThrow(request);

            SetMovieSessionIdIfNullOrChanged(request, cart);

            await EnsureSeatNotReserved(request);

            var seat = await movieSessionSeatService.GetSeat(request.MovieSessionId,
                request.SeatRow,
                request.SeatNumber,
                cancellationToken
            );

            // Add seat to shoppingCart
            DateTime expires = TimeProvider.System.GetUtcNow().DateTime.AddMinutes(2);
            var selectSeat = new SeatShoppingCart(request.SeatRow, request.SeatNumber, seat.Price, expires);
            cart.AddSeats(selectSeat, request.MovieSessionId);


            await SelectSaveSeatWithTimeoutRollback(request, cancellationToken, cart, expires);

            await SaveShoppingCart(cart);
        }

        return Result.Success();
    }

    private async Task EnsureSeatNotReserved(SelectSeatCommand request)
    {
        var reservedInRedis =
            await shoppingCartSeatLifecycleManager.IsSeatReservedAsync(request.MovieSessionId, request.SeatRow,
                request.SeatNumber);

        if (reservedInRedis)
        {
            throw new ConflictException($"Sear {request.MovieSessionId}:{request.SeatRow}:{request.SeatNumber}",
                nameof(SelectSeatCommandHandler));
        }
    }

    private async Task SelectSaveSeatWithTimeoutRollback(SelectSeatCommand request, CancellationToken cancellationToken,
        ShoppingCart cart, DateTime expires)
    {
        await movieSessionSeatService.SelectSeat(request.MovieSessionId,
            request.SeatRow,
            request.SeatNumber,
            request.ShoppingCartId,
            cart.HashId,
            cancellationToken
        );
        try
        {
            var result =
                await shoppingCartSeatLifecycleManager.SetAsync(request.MovieSessionId,
                    request.MovieSessionId,
                    request.SeatRow,
                    request.SeatNumber, expires);

            if (!result)
            {
                await ReturnSeatToAvailable(request, cancellationToken);
            }
        }
        catch (Exception e)
        {
            await ReturnSeatToAvailable(request, cancellationToken);
            throw;
        }
    }

    private async Task ReturnSeatToAvailable(SelectSeatCommand request, CancellationToken cancellationToken)
    {
        Logger.Error("Failed to set ShoppingCartSeat Lifecycle, try return MovieSessionSeat to Available");

        await movieSessionSeatService.ReturnToAvailable(request.MovieSessionId,
            request.SeatRow,
            request.SeatNumber,
            cancellationToken
        );
        Logger.Error("MovieSessionSeat returned to Available");
        throw new InvalidOperationException("Can't set ShoppingCartSeat Lifecycle. Try again later");
    }

    private void SetMovieSessionIdIfNullOrChanged(SelectSeatCommand request, ShoppingCart? shoppingCart)
    {
        if (shoppingCart.MovieSessionId != request.MovieSessionId)
        {
            shoppingCart.SetShowTime(request.MovieSessionId);
            Logger.Debug("MovieSessionId was updated updated {!ShoppingCart}", shoppingCart);
        }
    }

    private async Task<ShoppingCart> GetShoppingCartOrThrow(SelectSeatCommand request)
    {
        return await ActiveShoppingCartRepository.GetByIdAsync(request.ShoppingCartId) ??
               throw new ContentNotFoundException(nameof(ShoppingCart), request.ShoppingCartId.ToString());
    }

    private static void EnsureDistributedLockIsNotLocked(ILockHandler lockHandler, string lockKey)
    {
        if (!lockHandler.IsLocked)
        {
            throw new LockedException(lockKey, nameof(SelectSeatCommandHandler));
        }
    }
}