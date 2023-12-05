using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.PriceServices;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Application.ShoppingCarts.Base;

public class ActiveShoppingCartHandler
{
    
    protected readonly IActiveShoppingCartRepository ActiveShoppingCartRepository;

    protected readonly IShoppingCartLifecycleManager ShoppingCartLifecycleManager;

    public ActiveShoppingCartHandler(IActiveShoppingCartRepository activeShoppingCartRepository,
        IShoppingCartLifecycleManager shoppingCartLifecycleManager)
    {
        ActiveShoppingCartRepository = activeShoppingCartRepository;
        ShoppingCartLifecycleManager = shoppingCartLifecycleManager;
    }

    protected async Task SaveShoppingCart(ShoppingCart shoppingCart)
    {
        shoppingCart.CalculateCartAmount(new PriceService());
        await ActiveShoppingCartRepository.SaveAsync(shoppingCart);
        await ShoppingCartLifecycleManager.SetAsync(shoppingCart.Id);
    }
}