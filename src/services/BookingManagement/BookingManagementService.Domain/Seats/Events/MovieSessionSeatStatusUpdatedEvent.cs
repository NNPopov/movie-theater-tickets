using CinemaTicketBooking.Domain.Common.Events;

namespace CinemaTicketBooking.Domain.Seats.Events;

public class MovieSessionSeatStatusUpdatedEvent : IDomainEvent
{
    internal MovieSessionSeatStatusUpdatedEvent(MovieSessionSeat movieSessionSeat,
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