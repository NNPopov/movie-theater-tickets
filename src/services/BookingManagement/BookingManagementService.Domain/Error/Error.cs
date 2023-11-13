using CinemaTicketBooking.Domain.Exceptions;

namespace CinemaTicketBooking.Domain.Error;

public sealed record Error(string Code, string? Description = null)
{
    public static readonly Error None = new(string.Empty);
    
    public static implicit operator Result(Error error) => Result.Failure(error);
}