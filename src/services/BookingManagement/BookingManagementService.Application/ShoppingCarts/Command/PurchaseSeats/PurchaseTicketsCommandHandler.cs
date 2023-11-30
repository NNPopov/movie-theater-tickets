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

public class PurchaseTicketsCommandHandler : IRequestHandler<PurchaseTicketsCommand, Result>
{
    private ISeatStateRepository _seatStateRepository;

    private readonly MovieSessionSeatService _movieSessionSeatService;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;

    public PurchaseTicketsCommandHandler(
        ISeatStateRepository seatStateRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository, MovieSessionSeatService movieSessionSeatService)
    {
        _seatStateRepository = seatStateRepository;

        _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _movieSessionSeatService = movieSessionSeatService;
    }

    public async Task<Result> Handle(PurchaseTicketsCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await _shoppingCartRepository.GetByIdAsync(request.ShoppingCartId);

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
        await _shoppingCartRepository.SetAsync(cart);

        foreach (var seat in cart.Seats)
        {
            await _seatStateRepository.DeleteAsync(cart.MovieSessionId,seat.SeatRow,seat.SeatNumber);
        }

        return Result.Success();
    }
}