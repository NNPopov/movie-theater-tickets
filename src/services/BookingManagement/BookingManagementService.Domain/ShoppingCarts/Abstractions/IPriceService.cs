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

public record PriceCalculationResult (decimal TotalCartAmountBeforeDiscounts,
    decimal TotalCartAmountAfterDiscounts, 
    decimal TotalCartDiscounts, ICollection<AppliedPriceRule> AppliedPriceRules);

public record AppliedPriceRule( Guid Id ,decimal Discount);