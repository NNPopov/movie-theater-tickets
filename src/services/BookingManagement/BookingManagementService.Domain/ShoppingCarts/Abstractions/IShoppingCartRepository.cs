namespace CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

public interface IShoppingCartRepository
{
    Task<ShoppingCart> SetAsync(ShoppingCart shoppingCart);
    
    Task DeleteAsync(ShoppingCart shoppingCart);

    Task<ShoppingCart> GetByIdAsync(Guid cartId);
    
    Task<Guid> GetActiveShoppingCartByClientIdAsync(Guid clientId);

    Task SetClientActiveShoppingCartAsync(Guid clientId, Guid shoppingCartId);

}