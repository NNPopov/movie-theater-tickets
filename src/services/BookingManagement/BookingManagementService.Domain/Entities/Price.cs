using CinemaTicketBooking.Domain.Common;

namespace CinemaTicketBooking.Domain.Entities;

public class Price : ValueObject
{
    public Price(string currency, decimal amount)
    {
        Currency = currency;
        Amount = amount;
    }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}