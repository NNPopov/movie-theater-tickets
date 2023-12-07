namespace CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

public interface IPriceService
{
    public void AddRule(IPriceRule priceRule);
    
    public PriceCalculationResult GetCartAmount(ICollection<SeatShoppingCart> cartItems);
}

public interface IPriceRule
{
    public Guid Id { get; }
    
    public decimal Apply(ICollection<SeatShoppingCart> cartItems, decimal totalCartAmountBeforeDiscounts);
}

