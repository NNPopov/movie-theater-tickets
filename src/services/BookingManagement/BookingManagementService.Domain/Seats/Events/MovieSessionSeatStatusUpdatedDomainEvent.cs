using CinemaTicketBooking.Domain.Common.Events;

namespace CinemaTicketBooking.Domain.Seats.Events;

public sealed class MovieSessionSeatStatusUpdatedDomainEvent : IDomainEvent
{
    internal MovieSessionSeatStatusUpdatedDomainEvent(MovieSessionSeat movieSessionSeat,
        SeatStatus previousStatus
    )
    {
        MovieSessionId = movieSessionSeat.MovieSessionId;
        SeatNumber = movieSessionSeat.SeatNumber;
        SeatRow = movieSessionSeat.SeatRow;
        CurrentStatus = movieSessionSeat.Status;
        PreviousStatus = previousStatus;
    }


    public Guid MovieSessionId { get; }
    public short SeatNumber { get; }
    public short SeatRow { get; }

    public SeatStatus CurrentStatus { get; }
    public SeatStatus PreviousStatus { get; }
}