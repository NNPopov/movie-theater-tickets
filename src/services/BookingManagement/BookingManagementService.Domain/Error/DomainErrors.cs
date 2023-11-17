namespace CinemaTicketBooking.Domain.Error;

public static class DomainErrors<T>
{
    public static Domain.Error.Error ConflictException(string? description = null) => new ConflictError(
        $"{nameof(T)}.ConflictException",
        description);

    public static Domain.Error.Error NotFound(string? description = null) => new NotFountError(
        $"{nameof(T)}.NotFound",
        description);
    
    public static Domain.Error.Error DomainValidation(string description) => new(
        $"{nameof(T)}.DomainValidation",
        description);

    public static Domain.Error.Error InvalidOperation(string? description = null) => new(
        $"{nameof(T)}.InvalidOperation",
        description);
}