using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

namespace CinemaTicketBooking.Domain.PriceServices;

public class PriceService : IPriceService
{
    public void AddRule(IPriceRule priceRule)
    {
        throw new NotImplementedException();
    }

    public PriceCalculationResult GetCartAmount(ICollection<SeatShoppingCart> cartItems)
    {
        decimal totalCartAmountBeforeDiscounts = 0;
        decimal totalCartAmountAfterDiscounts = 0;
        decimal totalCartDiscounts = 0;

        foreach (var cartItem in cartItems)
        {
            totalCartAmountBeforeDiscounts += cartItem.Price;
        }

        totalCartAmountAfterDiscounts = totalCartAmountBeforeDiscounts - totalCartDiscounts;

        return new PriceCalculationResult(totalCartAmountBeforeDiscounts, totalCartAmountAfterDiscounts,
            totalCartDiscounts, new List<AppliedPriceRule>());
    }
}