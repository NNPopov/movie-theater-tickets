namespace CinemaTicketBooking.Application.Abstractions;

/// <summary>
/// Application port exposing the identity of the caller of the current request.
/// Implemented in the API composition root over <c>IHttpContextAccessor</c> so the
/// Application layer stays framework-free (no <c>HttpContext</c> dependency).
/// </summary>
public interface ICurrentUser
{
    /// <summary>True when the caller carries a valid identity claim.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The caller's client id, or <see cref="Guid.Empty"/> when not authenticated.</summary>
    Guid ClientId { get; }
}
