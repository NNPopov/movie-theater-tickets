using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;

public record CreateShoppingCartCommand(short MaxNumberOfSeats, Guid RequestId) : IdempotentRequest(RequestId),
    IRequest<CreateShoppingCartResponse>;

public class CreateShoppingCartCommandHandler : IRequestHandler<CreateShoppingCartCommand, CreateShoppingCartResponse>
{
    private ISeatStateRepository _seatStateRepository;

    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IShoppingCartNotifier _shoppingCartNotifier;

    public CreateShoppingCartCommandHandler(
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

    public async Task<CreateShoppingCartResponse> Handle(CreateShoppingCartCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingCart = ShoppingCart.Create(request.MaxNumberOfSeats);
        await _shoppingCartRepository.TrySetCart(shoppingCart);
        
        await _shoppingCartNotifier.SentShoppingCartState(shoppingCart);

        return new CreateShoppingCartResponse(shoppingCart.Id, shoppingCart.HashId);
    }
}