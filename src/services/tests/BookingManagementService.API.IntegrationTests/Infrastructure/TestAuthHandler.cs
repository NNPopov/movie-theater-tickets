using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CinemaTicketBooking.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Default authentication scheme for the test host. When the request carries
/// <c>X-Test-User: &lt;guid&gt;</c> it produces an authenticated principal whose
/// <c>nameidentifier</c> claim equals that guid — the exact claim CurrentUser / GetClientId read.
/// With no header it produces no result (anonymous), exactly like an unauthenticated caller.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";

    private const string NameIdentifier =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var values))
            return Task.FromResult(AuthenticateResult.NoResult());

        var raw = values.ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(new[] { new Claim(NameIdentifier, raw) }, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
