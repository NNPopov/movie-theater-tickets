using System.Xml.Schema;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;

public record SelectSeatCommand
    (Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId) : IRequest<bool>;

public record SeatDto(short Row, short Number);

public class SelectSeatCommandHandler : IRequestHandler<SelectSeatCommand, bool>
{
    private readonly MovieSessionSeatService _movieSessionSeatService;
    private IShoppingCartRepository _shoppingCartRepository;
    private ISeatStateRepository _seatStateRepository;
    private IDistributedLock _distributedLock;
    private readonly IShoppingCartNotifier _shoppingCartNotifier;
    private ILogger _logger;

    public SelectSeatCommandHandler(
        //IMovieSessionsRepository movieSessionsRepository,
        ISeatStateRepository seatStateRepository,
        //IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository,
        IDistributedLock distributedLock,
        IShoppingCartNotifier shoppingCartNotifier, ILogger logger,
        MovieSessionSeatService movieSessionSeatService)
    {
      //  _movieSessionsRepository = movieSessionsRepository;
        _seatStateRepository = seatStateRepository;
     //   _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _distributedLock = distributedLock;
        _shoppingCartNotifier = shoppingCartNotifier;
        _logger = logger;
        _movieSessionSeatService = movieSessionSeatService;
    }

    public async Task<bool> Handle(SelectSeatCommand request,
        CancellationToken cancellationToken)
    {
        var lockKey = $"lock:{request.MovieSessionId}:{request.SeatRow}:{request.SeatNumber}";

        ShoppingCart? cart;

        await using (var lockHandler = await _distributedLock.TryAcquireAsync(lockKey,
                         cancellationToken: cancellationToken))
        {
            if (!lockHandler.IsLocked)
                return false;

            cart = await _shoppingCartRepository.TryGetCart(request.ShoppingCartId);

            if (cart == null)
                throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));


            // var movieSession = await _movieSessionsRepository
            //     .GetWithTicketsByIdAsync(
            //         request.MovieSessionId, cancellationToken);
            //
            // if (movieSession == null)
            //     throw new ContentNotFoundException(request.MovieSessionId.ToString(), nameof(MovieSession));


            if (cart.MovieSessionId != request.MovieSessionId)
            {
                cart.SetShowTime(request.MovieSessionId);
            }

            //Step 1 Check is seat already reserved

            var reservedInRedis =
                await _seatStateRepository.GetAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber);

            if (!(reservedInRedis is null))
            {
                return false;
            }

            // Step 2 Select place
            cart.AddSeats(new SeatShoppingCart(request.SeatRow, request.SeatNumber), request.MovieSessionId);


            await _movieSessionSeatService.SelectSeat(request.MovieSessionId,
                request.SeatRow,
                request.SeatNumber,
                request.ShoppingCartId,
                cart.HashId,
                cancellationToken
            );

            // Step 3 Add seat to cart
            var seatReservationInfo = new SeatSelectedInfo
            (
                SeatRow: request.SeatRow,
                SeatNumber: request.SeatNumber,
                MovieSessionId: request.MovieSessionId,
                ShoppingCartId: request.ShoppingCartId
            );


            var result =
                await _seatStateRepository.SetAsync(seatReservationInfo, new TimeSpan(0, 0, 120));

            if (!result)
            {
                return false;
            }

            await _shoppingCartRepository.TrySetCart(cart);
        }

        await _shoppingCartNotifier.SentShoppingCartState(cart);

        return true;
    }
}

public record SeatSelectedInfo(Guid ShoppingCartId,
    Guid MovieSessionId,
    short SeatNumber,
    short SeatRow);