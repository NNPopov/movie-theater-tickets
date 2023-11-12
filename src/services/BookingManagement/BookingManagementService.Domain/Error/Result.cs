namespace CinemaTicketBooking.Domain.Error;

public class Result
{
    private Result(bool isSuccess, Domain.Error.Error error)
    {
        if (isSuccess && error != Domain.Error.Error.None || !isSuccess && error == Domain.Error.Error.None)
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }
    public bool IsSuccess { get; }
    
    public bool IsFailure => !IsSuccess;
    
    public Domain.Error.Error Error { get; }
    
    public static Result Success() => new(true, Domain.Error.Error.None);
    
    public static Result Failure(Domain.Error.Error error) => new(false, error);
}