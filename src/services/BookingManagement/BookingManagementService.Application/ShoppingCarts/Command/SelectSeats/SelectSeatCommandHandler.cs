using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;

public record SelectSeatCommand
    (Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId) : IRequest<Result>;

public class SelectSeatCommandHandler : IRequestHandler<SelectSeatCommand, Result>
{
    private readonly MovieSessionSeatService _movieSessionSeatService;
    private IShoppingCartRepository _shoppingCartRepository;
    private ISeatStateRepository _seatStateRepository;
    private IDistributedLock _distributedLock;
    private readonly IShoppingCartNotifier _shoppingCartNotifier;
    private ILogger _logger;

    public SelectSeatCommandHandler(
        ISeatStateRepository seatStateRepository,
        IShoppingCartRepository shoppingCartRepository,
        IDistributedLock distributedLock,
        IShoppingCartNotifier shoppingCartNotifier, ILogger logger,
        MovieSessionSeatService movieSessionSeatService)
    {
        _seatStateRepository = seatStateRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _distributedLock = distributedLock;
        _shoppingCartNotifier = shoppingCartNotifier;
        _logger = logger;
        _movieSessionSeatService = movieSessionSeatService;
    }

    public async Task<Result> Handle(SelectSeatCommand request,
        CancellationToken cancellationToken)
    {
        var lockKey = $"lock:{request.MovieSessionId}:{request.SeatRow}:{request.SeatNumber}";

        ShoppingCart? cart;

        await using (var lockHandler = await _distributedLock.TryAcquireAsync(lockKey,
                         cancellationToken: cancellationToken))
        {
            if (!lockHandler.IsLocked)
            {
                return DomainErrors<ShoppingCart>.ConflictException("Seat already reserved");
            }

            cart = await _shoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

            if (cart == null)
                return DomainErrors<ShoppingCart>.NotFound(request.ShoppingCartId.ToString());


            if (cart.MovieSessionId != request.MovieSessionId)
            {
                cart.SetShowTime(request.MovieSessionId);
            }

            //Step 1 Check is seat already reserved

            var reservedInRedis =
                await _seatStateRepository.GetAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber);

            if (!(reservedInRedis is null))
            {
                return DomainErrors<ShoppingCart>.ConflictException("Seat already reserved");
            }


            // Step Add seat to cart
            cart.AddSeats(new SeatShoppingCart(request.SeatRow, request.SeatNumber), request.MovieSessionId);


            // Step 2 Select place

            var result =
                await _seatStateRepository.SetAsync(request.MovieSessionId,
                    request.SeatRow,
                    request.SeatNumber,
                    new TimeSpan(0, 0, 120));

            if (!result)
            {
                return DomainErrors<ShoppingCart>.InvalidOperation("Can't save seat. Try again later");
            }

            await _movieSessionSeatService.SelectSeat(request.MovieSessionId,
                request.SeatRow,
                request.SeatNumber,
                request.ShoppingCartId,
                cart.HashId,
                cancellationToken
            );


            await _shoppingCartRepository.SetAsync(cart);
        }

        //await _shoppingCartNotifier.SentShoppingCartState(cart);

        return Result.Success();
    }
}
