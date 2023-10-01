using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.Exceptions;

namespace CinemaTicketBooking.Domain.Seats;

public class MovieSessionSeat : ValueObject
{
    public MovieSessionSeat(Guid movieSessionId, short seatNumber, short seatRow, decimal price)
    {
        Ensure.NotEmpty(movieSessionId, "The movieSessionId is required.", nameof(movieSessionId));
        Ensure.NotEmpty(seatNumber, "The seatNumber is required.", nameof(seatNumber));
        Ensure.NotEmpty(price, "The price is required.", nameof(price));
        Ensure.NotEmpty(seatRow, "The seatRow is required.", nameof(seatRow));

        MovieSessionId = movieSessionId;
        SeatNumber = seatNumber;
        Status = SeatStatus.Available;
        Price = price;
        SeatRow = seatRow;
    }

    public Guid MovieSessionId { get; private set; }

    public short SeatNumber { get; private set; }

    public short SeatRow { get; private set; }

    public decimal Price { get; private set; }

    public SeatStatus Status { get; private set; }

    public void Select(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (Status != SeatStatus.Available)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }

        if (shoppingCartId != ShoppingCartId && ShoppingCartId != Guid.Empty)
        {
            throw new InvalidOperationException("The seat is already selected by another shopping cart.");
        }
        
        if (ShoppingCartId == Guid.Empty)
        {
            ShoppingCartId = shoppingCartId;
        }

        Status = SeatStatus.Selected;
    }

    public void Reserve(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (Status != SeatStatus.Selected && Status != SeatStatus.Available) // || ShoppingCartId != shoppingCartId)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }

        Status = SeatStatus.Reserved;
    }

    public void Sel(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (ShoppingCartId != shoppingCartId)
        {
            throw new InvalidOperationException("The seat is already selected by another shopping cart.");
        }
        
        if (Status == SeatStatus.Sold)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }

        Status = SeatStatus.Sold;
        
    }

    public void ReturnToAvailable()
    {
        if (Status == SeatStatus.Sold)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }

        Status = SeatStatus.Available;
        ShoppingCartId = Guid.Empty;
        
    }

    public Guid ShoppingCartId { get; set; }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return MovieSessionId;

        yield return SeatNumber;

        yield return SeatRow;
    }

    public override string ToString()
    {
        return $"MovieSessionId:{MovieSessionId}, SeatRow:{SeatRow}, SeatNumber:{SeatNumber}, Status:{Status.ToString()}";
    }
}