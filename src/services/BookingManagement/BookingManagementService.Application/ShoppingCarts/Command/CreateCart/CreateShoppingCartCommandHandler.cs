using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;

public record CreateShoppingCartCommand(short MaxNumberOfSeats, Guid RequestId) : IdempotentRequest(RequestId),
    IRequest<CreateShoppingCartResponse>;

public class CreateShoppingCartCommandHandler : IRequestHandler<CreateShoppingCartCommand, CreateShoppingCartResponse>
{
    private ISeatStateRepository _seatStateRepository;

    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;

    public CreateShoppingCartCommandHandler(
        ISeatStateRepository seatStateRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository)
    {
        _seatStateRepository = seatStateRepository;

        _movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
    }

    public async Task<CreateShoppingCartResponse> Handle(CreateShoppingCartCommand request,
        CancellationToken cancellationToken)
    {
        var ticketCart = ShoppingCart.Create(request.MaxNumberOfSeats);
        await _shoppingCartRepository.TrySetCart(ticketCart);

        return new CreateShoppingCartResponse(ticketCart.Id);
    }
}