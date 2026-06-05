namespace CinemaTicketBooking.Domain.Error;

public static class DomainErrors<T>
{
    public static Domain.Error.Error ConflictException(string? description = null) => new ConflictError(
        $"{typeof(T).Name}.ConflictException",
        description);

    public static Domain.Error.Error NotFound(string? description = null) => new NotFoundError(
        $"{typeof(T).Name}.NotFound",
        description);

    public static Domain.Error.Error DomainValidation(string description) => new(
        $"{typeof(T).Name}.DomainValidation",
        description);

    public static Domain.Error.Error InvalidOperation(string? description = null) => new(
        $"{typeof(T).Name}.InvalidOperation",
        description);
}