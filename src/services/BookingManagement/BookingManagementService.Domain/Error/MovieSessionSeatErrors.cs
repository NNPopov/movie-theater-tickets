namespace CinemaTicketBooking.Domain.Error;

public static class MovieSessionSeatErrors
{
    public static readonly Domain.Error.Error ConflictException = new(
        "MovieSessionSeat.ConflictException",
        "Сan't assign an existing status");
    
    public static readonly Domain.Error.Error MovieSessionSeatNotFound = new(
        "MovieSessionSeat.NotFound",
        "Can't get a seat");
    
    public static readonly Domain.Error.Error MovieSessionSeatProcessed = new(
        "MovieSessionSeat.Processed",
        "The place is already being processed by another shopping cart");
}