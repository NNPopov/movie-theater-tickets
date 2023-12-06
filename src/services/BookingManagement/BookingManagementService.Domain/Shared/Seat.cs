using CinemaTicketBooking.Domain.Common;

namespace CinemaTicketBooking.Domain.Shared;

public abstract class Seat : ValueObject
{
    protected Seat(short seatRow, short seatNumber)
    {
        SeatRow = seatRow;
        SeatNumber = seatNumber;
    }

    public short SeatRow { get; private set; }
    public short SeatNumber { get; private set; }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return SeatNumber;

        yield return SeatRow;
    }
}