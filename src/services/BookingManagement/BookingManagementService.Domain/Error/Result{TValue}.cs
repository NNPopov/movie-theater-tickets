namespace CinemaTicketBooking.Domain.Error;

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value) : base(true, Error.None) => _value = value;

    private Result(Error error) : base(false, error) => _value = default;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static Result<TValue> Success(TValue value) => new(value);

    public static new Result<TValue> Failure(Error error) => new(error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
