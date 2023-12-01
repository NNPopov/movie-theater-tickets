using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Queries;

public record GetShoppingCartQuery(Guid ShoppingCartId) : IRequest<ShoppingCart>;

public class GetShoppingCartQueryHandler : IRequestHandler<GetShoppingCartQuery, ShoppingCart>
{
    private readonly IMapper _mapper;
    private IShoppingCartRepository _shoppingCartRepository;

    public GetShoppingCartQueryHandler(IMapper mapper,
        IShoppingCartRepository shoppingCartRepository)
    {
        _mapper = mapper;
        _shoppingCartRepository = shoppingCartRepository;
    }

    public async Task<ShoppingCart> Handle(GetShoppingCartQuery request,
        CancellationToken cancellationToken)
    {
        
        var cart = await _shoppingCartRepository.GetByIdAsync(request.ShoppingCartId);
        
        if (cart is null)
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));

        return cart;
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
    
    public PriceCalculationResult PriceCalculationResult { get;  set; }
    
    public bool IsAssigned { get; set; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ShoppingCart, ShoppingCartDto>()
                .ForMember(dst => dst.IsAssigned, opt => opt.MapFrom(src => src.ClientId != Guid.Empty));
        }
    }
}