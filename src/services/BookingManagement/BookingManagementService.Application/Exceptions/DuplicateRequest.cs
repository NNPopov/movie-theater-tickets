using FluentValidation.Results;

namespace CinemaTicketBooking.Application.Exceptions;

public class DuplicateRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DuplicateRequestException class with a specified name of the queried object and its key.
    /// </summary>
    /// <param name="objectName">Name of the queried object.</param>
    /// <param name="key">The value by which the object is queried.</param>
    public DuplicateRequestException(string key, string objectName)
        : base($"Queried object {objectName} was not found, Key: {key}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the DuplicateRequestException class with a specified name of the queried object, its key,
    /// and the exception that is the cause of this exception.
    /// </summary>
    /// <param name="objectName">Name of the queried object.</param>
    /// <param name="key">The value by which the object is queried.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DuplicateRequestException(string key, string objectName, Exception innerException)
        : base($"Queried object {objectName} was not found, Key: {key}", innerException)
    {
    }
}