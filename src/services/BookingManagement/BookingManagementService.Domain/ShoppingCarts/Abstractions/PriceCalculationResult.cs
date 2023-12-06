namespace CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

public record PriceCalculationResult (decimal TotalCartAmountBeforeDiscounts,
    decimal TotalCartAmountAfterDiscounts, 
    decimal TotalCartDiscounts, ICollection<AppliedPriceRule> AppliedPriceRules);
    
public record AppliedPriceRule( Guid Id ,decimal Discount);