namespace CinemaTicketBooking.Domain.Exceptions;

public class DomainValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the NotFoundException class with a specified name of the queried object and its key.
    /// </summary>
    /// <param name="message">Name of the queried object.</param>
    public DomainValidationException(string message)
        : base(message)
    {
    }
}