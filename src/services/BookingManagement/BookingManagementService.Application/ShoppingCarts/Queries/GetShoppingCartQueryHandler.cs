using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.ShoppingCarts.Queries;

public record GetShoppingCartQuery(Guid ShoppingCartId) : IRequest<ShoppingCartDto>;

public class GetShoppingCartQueryHandler : IRequestHandler<GetShoppingCartQuery, ShoppingCartDto>
{
    private readonly IMapper _mapper;
    private IShoppingCartRepository _shoppingCartRepository;

    public GetShoppingCartQueryHandler(IMapper mapper,
        IShoppingCartRepository shoppingCartRepository)
    {
        _mapper = mapper;
        _shoppingCartRepository = shoppingCartRepository;
    }

    public async Task<ShoppingCartDto> Handle(GetShoppingCartQuery request,
        CancellationToken cancellationToken)
    {
        
        var cart = await _shoppingCartRepository.TryGetCart(request.ShoppingCartId);
        
        if (cart is null)
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));

        return _mapper.Map<ShoppingCartDto>(cart);
    }
}

public class ShoppingCartDto
{
    public short MaxNumberOfSeats { get; set; }

    public DateTime CreatedCard { get; set; }
    
    public Guid Id { get; set; }
    
    public Guid MovieSessionId { get; set; }
    
    public ShoppingCartStatus Status { get;  set; }
    
    public IReadOnlyList<SeatShoppingCart> Seats { get; set; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ShoppingCart, ShoppingCartDto>();
        }
    }
}