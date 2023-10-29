using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ReserveSeats;

public record ReserveTicketsCommand( Guid ShoppingCartId) : IRequest<bool>;


public class ReserveTicketsCommandHandler : IRequestHandler<ReserveTicketsCommand, bool>
{
    private ISeatStateRepository _seatStateRepository;

    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IShoppingCartNotifier _shoppingCartNotifier;
    public ReserveTicketsCommandHandler(
        ISeatStateRepository seatStateRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository, 
        IShoppingCartNotifier shoppingCartNotifier)
    {
        _seatStateRepository = seatStateRepository;

        _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _shoppingCartNotifier = shoppingCartNotifier;
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
            var reservedSeatValue1 =
                await _movieSessionSeatRepository.GetByIdAsync(cart.MovieSessionId, seat.SeatRow, seat.SeatNumber,
                    cancellationToken);

            reservedSeatValue1.Reserve(request.ShoppingCartId);
            

            await _movieSessionSeatRepository.UpdateAsync(reservedSeatValue1, cancellationToken);
        }
        
        cart.SeatsReserve();
        await _shoppingCartRepository.TrySetCart(cart);

        foreach (var seat in cart.Seats)
        {
            await _seatStateRepository.DeleteAsync(cart.MovieSessionId,seat.SeatRow,seat.SeatNumber);
        }
        
        await _shoppingCartNotifier.SendShoppingCartState(cart);

        return true;
    }
}