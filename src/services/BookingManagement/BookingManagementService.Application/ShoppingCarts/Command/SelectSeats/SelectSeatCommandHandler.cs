using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.ShoppingCarts.Base;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.PriceServices;
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

        ShoppingCart? cart;

        await using (var lockHandler = await distributedLock.TryAcquireAsync(lockKey,
                         cancellationToken: cancellationToken))
        {
            if (!lockHandler.IsLocked)
            {
                return DomainErrors<ShoppingCart>.ConflictException("Seat already reserved");
            }

            cart = await ActiveShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

            if (cart == null)
                return DomainErrors<ShoppingCart>.NotFound(request.ShoppingCartId.ToString());


            if (cart.MovieSessionId != request.MovieSessionId)
            {
                cart.SetShowTime(request.MovieSessionId);
            }

            //Step 1 Check is seat already reserved

            var reservedInRedis =
                await shoppingCartSeatLifecycleManager.GetAsync(request.MovieSessionId, request.SeatRow,
                    request.SeatNumber);

            if (!(reservedInRedis is null))
            {
                return DomainErrors<ShoppingCart>.ConflictException("Seat already reserved");
            }

            var seat = await movieSessionSeatService.SelectSeat(request.MovieSessionId,
                request.SeatRow,
                request.SeatNumber,
                request.ShoppingCartId,
                cart.HashId,
                cancellationToken
            );

            DateTime expires
                = TimeProvider.System.GetUtcNow().DateTime.AddMinutes(2);
            // Step Add seat to cart
            var selectSeat = new SeatShoppingCart(request.SeatRow, request.SeatNumber, seat.Price, expires);
            cart.AddSeats(selectSeat, request.MovieSessionId);

            cart.CalculateCartAmount(new PriceService());

            // Step 2 Select place

            var result =
                await shoppingCartSeatLifecycleManager.SetAsync(request.MovieSessionId,
                    selectSeat);

            if (!result)
            {
                return DomainErrors<ShoppingCart>.InvalidOperation("Can't save seat. Try again later");
            }

            await SaveShoppingCart(cart);

            // await activeShoppingCartRepository.SaveAsync(cart);
            // await shoppingCartLifecycleManager.SetAsync(cart.Id);
        }

        //await _shoppingCartNotifier.SentShoppingCartState(cart);

        return Result.Success();
    }
}