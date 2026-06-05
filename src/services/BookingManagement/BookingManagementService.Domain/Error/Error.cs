using CinemaTicketBooking.Domain.Exceptions;

namespace CinemaTicketBooking.Domain.Error;

public record Error(string Code, string? Description = null)
{
    public static readonly Error None = new(string.Empty);

    public static implicit operator Result(Error error) => Result.Failure(error);
}

public sealed record ConflictError(string Code, string? Description = null) : Error(Code, Description);


public sealed record NotFoundError(string Code, string? Description = null) : Error(Code, Description);