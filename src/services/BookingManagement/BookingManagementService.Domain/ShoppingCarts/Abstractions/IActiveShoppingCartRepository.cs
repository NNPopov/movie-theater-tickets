namespace CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

public interface IActiveShoppingCartRepository
{
    Task<ShoppingCart> SaveAsync(ShoppingCart shoppingCart);
    
    Task DeleteAsync(ShoppingCart shoppingCart);

    Task<ShoppingCart> GetByIdAsync(Guid shoppingCartId);
    
    Task<Guid> GetActiveShoppingCartByClientIdAsync(Guid clientId);

    Task SetClientActiveShoppingCartAsync(Guid clientId, Guid shoppingCartId);

}