using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ReserveSeats;

public record ReserveTicketsCommand( Guid ShoppingCartId) : IRequest<bool>;


public class ReserveTicketsCommandHandler : IRequestHandler<ReserveTicketsCommand, bool>
{
    private ISeatStateRepository _seatStateRepository;

    private readonly MovieSessionSeatService _movieSessionSeatService;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IShoppingCartNotifier _shoppingCartNotifier;
    public ReserveTicketsCommandHandler(
        ISeatStateRepository seatStateRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository, 
        IShoppingCartNotifier shoppingCartNotifier, MovieSessionSeatService movieSessionSeatService)
    {
        _seatStateRepository = seatStateRepository;

        _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _shoppingCartNotifier = shoppingCartNotifier;
        _movieSessionSeatService = movieSessionSeatService;
    }

    public async Task<bool> Handle(ReserveTicketsCommand request,
        CancellationToken cancellationToken)
    {
        // var showtime = await _movieSessionsRepository
        //     .GetWithTicketsByIdAsync(
        //         request.ShoppingCartId, cancellationToken);
        //
        // if (showtime == null)
        //     throw new Exception();


        var cart = await _shoppingCartRepository.TryGetCart(request.ShoppingCartId);

        if (cart == null)
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));

        foreach (var seat in cart.Seats)
        {
            // var reservedSeatValue1 =
            //     await _movieSessionSeatRepository.GetByIdAsync(cart.MovieSessionId, seat.SeatRow, seat.SeatNumber,
            //         cancellationToken);

         await   _movieSessionSeatService.ReserveSeat(cart.MovieSessionId, 
                seat.SeatRow,
                seat.SeatNumber,
                request.ShoppingCartId,
                cancellationToken);
            

         //   await _movieSessionSeatRepository.UpdateAsync(reservedSeatValue1, cancellationToken);
        }
        
        cart.SeatsReserve();
        await _shoppingCartRepository.TrySetCart(cart);

        foreach (var seat in cart.Seats)
        {
            await _seatStateRepository.DeleteAsync(cart.MovieSessionId,seat.SeatRow,seat.SeatNumber);
        }
        
        await _shoppingCartNotifier.SentShoppingCartState(cart);

        return true;
    }
}