using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.PurchaseSeats;

public record PurchaseTicketsCommand(Guid ShoppingCartId) : IRequest<Result>;

internal sealed  class PurchaseTicketsCommandHandler : IRequestHandler<PurchaseTicketsCommand, Result>
{
    private IShoppingCartSeatLifecycleManager _shoppingCartSeatLifecycleManager;

    private readonly MovieSessionSeatService _movieSessionSeatService;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly IActiveShoppingCartRepository _activeShoppingCartRepository;
    private readonly IShoppingCartLifecycleManager _shoppingCartLifecycleManager;

    public PurchaseTicketsCommandHandler(
        IShoppingCartSeatLifecycleManager shoppingCartSeatLifecycleManager,
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IActiveShoppingCartRepository activeShoppingCartRepository,
        MovieSessionSeatService movieSessionSeatService,
        IShoppingCartLifecycleManager shoppingCartLifecycleManager)
    {
        _shoppingCartSeatLifecycleManager = shoppingCartSeatLifecycleManager;

        _movieSessionSeatRepository = movieSessionSeatRepository;
        _activeShoppingCartRepository = activeShoppingCartRepository;
        _movieSessionSeatService = movieSessionSeatService;
        _shoppingCartLifecycleManager = shoppingCartLifecycleManager;
    }

    public async Task<Result> Handle(PurchaseTicketsCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await _activeShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

        if (cart == null)
        {
            return DomainErrors<ShoppingCart>.NotFound(request.ShoppingCartId.ToString());
        }

        var result = await _movieSessionSeatService.SelSeats(cart.MovieSessionId,
            cart.Seats.Select(t => (t.SeatRow, t.SeatNumber)).ToList(),
            request.ShoppingCartId,
            cancellationToken);

        if (result.IsFailure)
        {
            return result;
        }


        cart.PurchaseComplete();
        await _activeShoppingCartRepository.SaveAsync(cart);
        await _shoppingCartLifecycleManager.DeleteAsync(cart.Id);

        foreach (var seat in cart.Seats)
        {
            await _shoppingCartSeatLifecycleManager.DeleteAsync(cart.MovieSessionId,seat.SeatRow,seat.SeatNumber);
        }

        return Result.Success();
    }
}