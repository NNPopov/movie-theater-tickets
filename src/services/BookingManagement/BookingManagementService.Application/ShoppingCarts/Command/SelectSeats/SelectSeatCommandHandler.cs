using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.ShoppingCarts;

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

    public SelectSeatCommandHandler(
        IMovieSessionsRepository movieSessionsRepository,
        ISeatStateRepository seatStateRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository,
        IDistributedLock distributedLock)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _seatStateRepository = seatStateRepository;
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _distributedLock = distributedLock;
    }

    public async Task<bool> Handle(SelectSeatCommand request,
        CancellationToken cancellationToken)
    {
        var lockKey = $"lock:{request.MovieSessionId}:{request.SeatRow}:{request.SeatNumber}";

        await using var lockHandler = await _distributedLock.TryAcquireAsync(lockKey);
        
        if (!lockHandler.IsLocked)
            return false;

        var cart = await _shoppingCartRepository.TryGetCart(request.ShoppingCartId);

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

        if (!movieSessionSeat.TrySelect(request.ShoppingCartId))
        {
            return false;
        }

        await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);

        // Step 3 Add seat to cart
        var seatReservationInfo = new SeatSelectedInfo
        {
            SeatRow = request.SeatRow,
            SeatNumber = request.SeatNumber,
            MovieSessionId = request.MovieSessionId,
            ShoppingCartId = request.ShoppingCartId
        };

        cart.AddSeats(new SeatShoppingCart(request.SeatRow, request.SeatNumber));
        var result =
            await _seatStateRepository.SetAsync(seatReservationInfo, new TimeSpan(0, 0, 120));

        if (!result)
        {
            return false;
        }

        await _shoppingCartRepository.TrySetCart(cart);

        // return result
        return true;
    }
}

public record ReserveResponse(List<short> ReservedSeats, List<short> BlockedSeats);
//
// public interface ISeatsReservationService
// {
//     Task RemoveReserveSeats(Guid showtimeId,
//         SeatShoppingCart seats,
//         Guid cartId);
//
//     Task<SelectSeatResponse> ReserveSeats(Guid showtimeId,
//         short row,
//         short number,
//         Guid cartId,
//         CancellationToken cancellationToken);
//
//     Task<SelectSeatResponse> BuySeats(Guid showtimeId,
//         SeatDto[] seats,
//         Guid userId,
//         MovieSession showtime,
//         CancellationToken cancellationToken);
// }
//
// public class SeatsReservationService : ISeatsReservationService
// {
//     private ISeatStateRepository _seatStateRepository;
//
//
//     private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
//
//     public SeatsReservationService(ISeatStateRepository seatStateRepository,
//         IMovieSessionSeatRepository movieSessionSeatRepository)
//     {
//         _seatStateRepository = seatStateRepository;
//         _movieSessionSeatRepository = movieSessionSeatRepository;
//     }
//
//
//     public async Task RemoveReserveSeats(Guid showtimeId,
//         SeatShoppingCart seats,
//         Guid cartId)
//     {
//         var cart = await _seatStateRepository.TryGetCart(cartId);
//
//         if (cart.ShoppingCartId != showtimeId)
//         {
//             //cart.SetShowTime(showtimeId);
//         }
//
//         cart.TryRemoveSeats(seats);
//
//         await _seatStateRepository.TrySetCart(cart);
//     }
//
//     public async Task<SelectSeatResponse> ReserveSeats(Guid showtimeId,
//         short row,
//         short number,
//         Guid cartId,
//         CancellationToken cancellationToken)
//     {
//         var reservedSeats = new List<SeatDto>();
//         var blockedSeats = new List<SeatDto>();
//
//
//         var cart = await _seatStateRepository.TryGetCart(cartId);
//
//         if (cart.ShoppingCartId != showtimeId)
//         {
//             cart.SetShowTime(showtimeId);
//         }
//
//
//         var reservedSeatKey = $"reservedseat:{showtimeId}:{row}:{number}:{cartId}";
//
//         var reservedInRedis = await _seatStateRepository.GetAsync<SeatSelectedInfo>(reservedSeatKey);
//
//         if (reservedInRedis != null)
//         {
//             if (reservedInRedis.ShoppingCartId != cartId)
//             {
//                 return new SelectSeatResponse(reservedSeats, blockedSeats, false);
//             }
//
//             reservedSeats.Add(new SeatDto(row, number));
//             return new SelectSeatResponse(reservedSeats, blockedSeats, true);
//         }
//
//         var reservedSeatValue =
//             await _movieSessionSeatRepository.GetByIdAsync(showtimeId, row, number, cancellationToken);
//
//
//         if (reservedSeatValue.Status != SeatStatus.Available)
//         {
//             return new SelectSeatResponse(default, default, false);
//         }
//
//
//         if (!reservedSeatValue.TrySelect(cartId))
//         {
//             return new SelectSeatResponse(reservedSeats, blockedSeats, false);
//         }
//
//
//         var seatReservationInfo = new SeatSelectedInfo
//         {
//             SeatRow = row,
//             SeatNumber = number,
//             ShoppingCartId = showtimeId,
//             ShoppingCartId = cartId
//         };
//
//         var result =
//             await _seatStateRepository.SetAsync(reservedSeatKey, seatReservationInfo, new TimeSpan(0, 0, 120));
//
//         if (!result)
//         {
//             return new SelectSeatResponse(default, default, false);
//         }
//
//         cart.AddSeats(new SeatShoppingCart(row, number));
//         await _seatStateRepository.TrySetCart(cart);
//         return new SelectSeatResponse(reservedSeats, blockedSeats, true);
//         
//     }
//
//     public async Task<SelectSeatResponse> BuySeats(Guid showtimeId,
//         SeatDto[] seats,
//         Guid userId,
//         MovieSession showtime,
//         CancellationToken cancellationToken)
//     {
//         // var reserveSeats = await ReserveSeats(showtimeId,
//         //     seats,
//         //     userId);
//         //
//         // if (reserveSeats.BlockedSeats.Count != 0)
//         //     throw new Exception();
//         //
//         // if (reserveSeats.ReservedSeats.Count != seats.Length)
//         //     throw new Exception();
//         //
//         //
//         // var seatEntityes = reserveSeats.ReservedSeats.Select(t => new ShowTimeSeatEntity
//         // {
//         //     SeatNumber = t.SeatNumber,
//         //     SeatRow = t.SeatRow,
//         //     AuditoriumId = showtime.AuditoriumId,
//         // });
//
//
//         //await ticketsRepository.CreateAsync(showtime, seatEntityes, cancellationToken);
//
//         return default;
//     }
// }

public class SeatSelectedInfo
{
    public Guid ShoppingCartId { get; set; }

    public Guid MovieSessionId { get; set; }

    public short SeatNumber { get; set; }

    public short SeatRow { get; set; }
}