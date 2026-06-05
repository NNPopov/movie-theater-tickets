using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Api.Authentication;

/// <summary>
/// <see cref="ICurrentUser"/> implementation over <see cref="IHttpContextAccessor"/>. Reads the
/// same <c>nameidentifier</c> claim as the endpoints' <c>GetClientId</c>, but returns
/// <em>anonymous</em> (no throw) when the claim is missing or not a <see cref="Guid"/> — the
/// conditional ownership check, not the identity reader, decides the 403.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private const string NameIdentifier =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

    public bool IsAuthenticated => TryGetClientId(out _);

    public Guid ClientId => TryGetClientId(out var id) ? id : Guid.Empty;

    private bool TryGetClientId(out Guid clientId)
    {
        var raw = accessor.HttpContext?.User.FindFirst(NameIdentifier)?.Value;
        return Guid.TryParse(raw, out clientId);
    }
}
