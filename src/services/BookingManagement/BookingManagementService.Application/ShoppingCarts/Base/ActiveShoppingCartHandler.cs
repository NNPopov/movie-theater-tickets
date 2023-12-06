using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.PriceServices;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.ShoppingCarts.Base;

public class ActiveShoppingCartHandler
{
    protected readonly IActiveShoppingCartRepository ActiveShoppingCartRepository;

    protected readonly IShoppingCartLifecycleManager ShoppingCartLifecycleManager;
    
    protected readonly ILogger Logger;

    public ActiveShoppingCartHandler(IActiveShoppingCartRepository activeShoppingCartRepository,
        IShoppingCartLifecycleManager shoppingCartLifecycleManager, ILogger logger)
    {
        ActiveShoppingCartRepository = activeShoppingCartRepository;
        ShoppingCartLifecycleManager = shoppingCartLifecycleManager;
        Logger = logger;
    }

    protected async Task SaveShoppingCart(ShoppingCart shoppingCart)
    {
        shoppingCart.CalculateCartAmount(new PriceService());
        await ActiveShoppingCartRepository.SaveAsync(shoppingCart);
        await ShoppingCartLifecycleManager.SetAsync(shoppingCart.Id);
        
        Logger.Debug("ShoppingCart was saved, ShoppingCartLifecycle was reset, Amount was recalculated {@ShoppingCart}", shoppingCart);
    }
}