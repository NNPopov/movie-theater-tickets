using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Application.ShoppingCarts.Command.AssingClientCart;
using CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Queries;

public record GetCurrentShoppingCartQuery(Guid ClientId) : IRequest<CreateShoppingCartResponse>;

public class GetCurrentShoppingCartQueryHandler(IMapper mapper,
        IShoppingCartRepository shoppingCartRepository)
    : IRequestHandler<GetCurrentShoppingCartQuery, CreateShoppingCartResponse>
{
    private readonly IMapper _mapper = mapper;

    public async Task<CreateShoppingCartResponse> Handle(GetCurrentShoppingCartQuery request,
        CancellationToken cancellationToken)
    {
        var existingShoppingCartId = await shoppingCartRepository.GetActiveShoppingCartByClientIdAsync(request.ClientId);

        if (existingShoppingCartId == Guid.Empty)
        {
            throw new ContentNotFoundException(request.ClientId.ToString(), nameof(ShoppingCart));
           
        }
        
        var shoppingCart = await shoppingCartRepository.GetByIdAsync(existingShoppingCartId);
        
        if (shoppingCart is null)
            throw new ContentNotFoundException(request.ClientId.ToString(), nameof(ShoppingCart));

        return new CreateShoppingCartResponse(shoppingCart.Id, shoppingCart.HashId);
    }
}

