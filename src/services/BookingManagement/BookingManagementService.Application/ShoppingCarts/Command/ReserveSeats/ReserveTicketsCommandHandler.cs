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


internal sealed  class ReserveTicketsCommandHandler : IRequestHandler<ReserveTicketsCommand, Result>
{
    private IShoppingCartSeatLifecycleManager _shoppingCartSeatLifecycleManager;

    private readonly MovieSessionSeatService _movieSessionSeatService;
    private readonly IActiveShoppingCartRepository _activeShoppingCartRepository;
    private readonly IShoppingCartLifecycleManager _shoppingCartLifecycleManager;
    private readonly IShoppingCartNotifier _shoppingCartNotifier;
    public ReserveTicketsCommandHandler(
        IShoppingCartSeatLifecycleManager shoppingCartSeatLifecycleManager,
        IActiveShoppingCartRepository activeShoppingCartRepository, 
        IShoppingCartNotifier shoppingCartNotifier, 
        MovieSessionSeatService movieSessionSeatService, IShoppingCartLifecycleManager shoppingCartLifecycleManager)
    {
        _shoppingCartSeatLifecycleManager = shoppingCartSeatLifecycleManager;
        
        _activeShoppingCartRepository = activeShoppingCartRepository;
        _shoppingCartNotifier = shoppingCartNotifier;
        _movieSessionSeatService = movieSessionSeatService;
        _shoppingCartLifecycleManager = shoppingCartLifecycleManager;
    }

    public async Task<Result> Handle(ReserveTicketsCommand request,
        CancellationToken cancellationToken)
    {

        var cart = await _activeShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);
        
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
        await _activeShoppingCartRepository.SaveAsync(cart);
        await _shoppingCartLifecycleManager.SetAsync(cart.Id);

        foreach (var seat in cart.Seats)
        {
            await _shoppingCartSeatLifecycleManager.DeleteAsync(cart.MovieSessionId,seat.SeatRow,seat.SeatNumber);
        }
        
        return Result.Success();
    }
}