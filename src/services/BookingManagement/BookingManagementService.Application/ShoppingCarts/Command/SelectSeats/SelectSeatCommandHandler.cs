using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;

public record SelectSeatCommand(Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId) : IRequest<bool>;

public record SeatDto(short Row, short Number);

public class SelectSeatCommandHandler : IRequestHandler<SelectSeatCommand, bool>
{
    private IMovieSessionsRepository _movieSessionsRepository;
    private IShoppingCartRepository _shoppingCartRepository;

    private ISeatStateRepository _seatStateRepository;
    
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private IDistributedLock _distributedLock;
    
    private readonly IShoppingCartNotifier _shoppingCartNotifier;
    private ILogger _logger;
    public SelectSeatCommandHandler(
        IMovieSessionsRepository movieSessionsRepository,
        ISeatStateRepository seatStateRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository,
        IDistributedLock distributedLock,
        IShoppingCartNotifier shoppingCartNotifier, ILogger logger)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _seatStateRepository = seatStateRepository;
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _distributedLock = distributedLock;
        _shoppingCartNotifier = shoppingCartNotifier;
        _logger = logger;
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
            

            var movieSession = await _movieSessionsRepository
                .GetWithTicketsByIdAsync(
                    request.MovieSessionId, cancellationToken);

            if (movieSession == null)
                throw new ContentNotFoundException(request.MovieSessionId.ToString(), nameof(MovieSession));

            
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
            var movieSessionSeat =
                await _movieSessionSeatRepository.GetByIdAsync(request.MovieSessionId, request.SeatRow,
                    request.SeatNumber, cancellationToken);

            if (movieSessionSeat is null)
                throw new Exception();
            
            cart.AddSeats(new SeatShoppingCart(request.SeatRow, request.SeatNumber), request.MovieSessionId);

            movieSessionSeat.Select(request.ShoppingCartId, cart.HashId);

            await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);
            
            _logger.Information("MovieSessionSeat was selected {@movieSessionSeat}", movieSessionSeat);

            // Step 3 Add seat to cart
            var seatReservationInfo = new SeatSelectedInfo
            {
                SeatRow = request.SeatRow,
                SeatNumber = request.SeatNumber,
                MovieSessionId = request.MovieSessionId,
                ShoppingCartId = request.ShoppingCartId
            };


            var result =
                await _seatStateRepository.SetAsync(seatReservationInfo, new TimeSpan(0, 0, 120));

            if (!result)
            {
                return false;
            }

            await _shoppingCartRepository.TrySetCart(cart);
        }
        
        await _shoppingCartNotifier.SendShoppingCartState(cart);

        // return result
        return true;
    }
}

public record ReserveResponse(List<short> ReservedSeats, List<short> BlockedSeats);


public class SeatSelectedInfo
{
    public Guid ShoppingCartId { get; set; }

    public Guid MovieSessionId { get; set; }

    public short SeatNumber { get; set; }

    public short SeatRow { get; set; }
}