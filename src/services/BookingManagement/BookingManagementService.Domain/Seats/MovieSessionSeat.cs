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

    public bool TrySelect(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (Status != SeatStatus.Available)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }

        if (ShoppingCartId == Guid.Empty)
        {
            ShoppingCartId = shoppingCartId;
        }
        else if (shoppingCartId != ShoppingCartId)
        {
            return false;
        }

        Status = SeatStatus.Selected;

        return true;
    }

    public bool TryReserve(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (Status != SeatStatus.Selected && Status != SeatStatus.Available) // || ShoppingCartId != shoppingCartId)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }

        Status = SeatStatus.Reserved;

        return true;
    }

    public bool TrySel(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (Status != SeatStatus.Reserved || ShoppingCartId != shoppingCartId)
        {
            return false;
        }
        
        if (Status == SeatStatus.Sold)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }

        Status = SeatStatus.Sold;

        return true;
    }

    public bool TryReturnToAvailable()
    {
        if (Status == SeatStatus.Sold)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }

        Status = SeatStatus.Available;
        ShoppingCartId = Guid.Empty;

        return true;
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