using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ReserveSeats;

public record ReserveTicketsCommand( Guid ShoppingCartId) : IRequest<Result>;


public class ReserveTicketsCommandHandler : IRequestHandler<ReserveTicketsCommand, Result>
{
    private ISeatStateRepository _seatStateRepository;

    private readonly MovieSessionSeatService _movieSessionSeatService;
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IShoppingCartNotifier _shoppingCartNotifier;
    public ReserveTicketsCommandHandler(
        ISeatStateRepository seatStateRepository,
        IShoppingCartRepository shoppingCartRepository, 
        IShoppingCartNotifier shoppingCartNotifier, 
        MovieSessionSeatService movieSessionSeatService)
    {
        _seatStateRepository = seatStateRepository;
        
        _shoppingCartRepository = shoppingCartRepository;
        _shoppingCartNotifier = shoppingCartNotifier;
        _movieSessionSeatService = movieSessionSeatService;
    }

    public async Task<Result> Handle(ReserveTicketsCommand request,
        CancellationToken cancellationToken)
    {

        var cart = await _shoppingCartRepository.GetByIdAsync(request.ShoppingCartId);
        
        if (cart == null)
        {
            return DomainErrors<ShoppingCart>.NotFound(request.ShoppingCartId.ToString());
        }

       
        var result = await   _movieSessionSeatService.ReserveSeats(cart.MovieSessionId, 
             cart.Seats.Select(t=>(t.SeatRow,t.SeatNumber)).ToList(),
                request.ShoppingCartId,
                cancellationToken);
        
        if (result.IsFailure)
        {
            return result;
        }
        
        cart.SeatsReserve();
        await _shoppingCartRepository.SetAsync(cart);

        foreach (var seat in cart.Seats)
        {
            await _seatStateRepository.DeleteAsync(cart.MovieSessionId,seat.SeatRow,seat.SeatNumber);
        }
        
        await _shoppingCartNotifier.SentShoppingCartState(cart);

        return Result.Success();
    }
}