using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ReserveSeats;

public record ReserveTicketsCommand(Guid ShoppingCartId) : IRequest<Result>;


internal sealed class ReserveTicketsCommandHandler : IRequestHandler<ReserveTicketsCommand, Result>
{
    private IShoppingCartSeatLifecycleManager _shoppingCartSeatLifecycleManager;

    private readonly MovieSessionSeatService _movieSessionSeatService;
    private readonly IActiveShoppingCartRepository _activeShoppingCartRepository;
    private readonly IShoppingCartLifecycleManager _shoppingCartLifecycleManager;
    private readonly ILogger _logger;
    public ReserveTicketsCommandHandler(
        IShoppingCartSeatLifecycleManager shoppingCartSeatLifecycleManager,
        IActiveShoppingCartRepository activeShoppingCartRepository,
        MovieSessionSeatService movieSessionSeatService, IShoppingCartLifecycleManager shoppingCartLifecycleManager, ILogger logger)
    {
        _shoppingCartSeatLifecycleManager = shoppingCartSeatLifecycleManager;

        _activeShoppingCartRepository = activeShoppingCartRepository;

        _movieSessionSeatService = movieSessionSeatService;
        _shoppingCartLifecycleManager = shoppingCartLifecycleManager;
        _logger = logger;
    }

    public async Task<Result> Handle(ReserveTicketsCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await _activeShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);
        if (cart is null)
            return DomainErrors<ShoppingCart>.NotFound(request.ShoppingCartId.ToString());

        var reserveResult = cart.SeatsReserve();
        if (reserveResult.IsFailure)
            return reserveResult;

        var result = await _movieSessionSeatService.ReserveSeats(cart.MovieSessionId,
             cart.Seats.Select(t => (t.SeatRow, t.SeatNumber)).ToList(),
                request.ShoppingCartId,
                cancellationToken);

        if (result.IsFailure)
            return result;

        await _activeShoppingCartRepository.SaveAsync(cart);
        await _shoppingCartLifecycleManager.SetAsync(cart.Id);

        foreach (var seat in cart.Seats)
        {
            await _shoppingCartSeatLifecycleManager.DeleteAsync(cart.MovieSessionId, seat.SeatRow, seat.SeatNumber);
        }

        _logger.Debug("ShoppingCart was reserved {@ShoppingCart}", cart);
        return Result.Success();
    }
}