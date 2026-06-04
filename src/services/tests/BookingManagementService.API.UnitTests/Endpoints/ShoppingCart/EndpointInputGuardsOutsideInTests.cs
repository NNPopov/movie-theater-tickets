using System.Security.Claims;
using System.Text.Json;
using CinemaTicketBooking.Api.Endpoints;
using CinemaTicketBooking.Api.Infrastructure;
using CinemaTicketBooking.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.UnitTests.Endpoints.ShoppingCart;

// Acceptance gate for slice 0008_endpoint_input_guards (ADR-002 endpoint-helper tail).
// Per the slice's tests.md (and the 0002 precedent), there is no WebApplicationFactory harness:
// the slice's externally-observable change is exercised through two seams — the internal static
// guards that replace the two bare `throw new Exception(...)` (ParseIdempotencyKey, GetClientId),
// and the central CustomExceptionHandler translation of the typed exceptions they raise to
// 400 / 401. The guard halves are RED until the guards are extracted/retyped per plan.md §5
// (ParseIdempotencyKey does not exist yet; GetClientId is currently private and throws a bare
// Exception). The CustomExceptionHandler mapping halves are characterizations (GREEN before and
// after) that pin the 400/401 contract the guards rely on.
public class EndpointInputGuardsOutsideInTests
{
    private const string NameIdentifierClaim =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static CustomExceptionHandler CreateHandler()
        => new(Substitute.For<ILogger>());

    private static HttpContext CreateHttpContextWithBuffer()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ProblemDetails?> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, WebJson);
    }

    // Scenario 1 — malformed X-Idempotency-Key => DomainValidationException => 400 (RED gate).
    [Fact]
    public async Task Malformed_idempotency_key_throws_DomainValidationException_and_maps_to_400()
    {
        // Arrange / Act — guard half (RED until ParseIdempotencyKey exists)
        var malformed = () => ShoppingCartEndpointApplicationBuilderExtensions.ParseIdempotencyKey("not-a-guid");
        var empty = () => ShoppingCartEndpointApplicationBuilderExtensions.ParseIdempotencyKey(string.Empty);

        // Assert — guard throws the specific typed exception, not a bare Exception
        malformed.Should().Throw<DomainValidationException>();
        empty.Should().Throw<DomainValidationException>();

        // Arrange — translation half (characterization: existing DomainValidationException => 400)
        var handler = CreateHandler();
        var httpContext = CreateHttpContextWithBuffer();
        var exception = new DomainValidationException("Invalid idempotency key: not-a-guid");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert — status + ProblemDetails shape
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problem = await ReadProblemDetailsAsync(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
    }

    // Scenario 2 — non-Guid / missing nameidentifier claim => UnauthorizedAccessException => 401 (RED gate).
    [Fact]
    public async Task Bad_or_missing_nameidentifier_claim_throws_Unauthorized_and_maps_to_401()
    {
        // Arrange
        var principalWithBadClaim = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(NameIdentifierClaim, "not-a-guid") }));
        var principalWithNoClaim = new ClaimsPrincipal(new ClaimsIdentity());

        // Act — guard half (RED until GetClientId is internal and throws UnauthorizedAccessException)
        var badClaim = () => ShoppingCartEndpointApplicationBuilderExtensions.GetClientId(principalWithBadClaim);
        var noClaim = () => ShoppingCartEndpointApplicationBuilderExtensions.GetClientId(principalWithNoClaim);

        // Assert — guard throws the specific typed exception
        badClaim.Should().Throw<UnauthorizedAccessException>();
        noClaim.Should().Throw<UnauthorizedAccessException>();

        // Arrange — translation half (characterization: existing UnauthorizedAccessException => 401)
        var handler = CreateHandler();
        var httpContext = CreateHttpContextWithBuffer();
        var exception = new UnauthorizedAccessException("Invalid nameidentifier claim: not-a-guid");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert — status + ProblemDetails shape
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var problem = await ReadProblemDetailsAsync(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problem.Title.Should().Be("Unauthorized");
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7235#section-3.1");
    }

    // Scenario 3 — valid key / valid claim => the parsed Guid (happy path; GREEN after implementation).
    [Fact]
    public void Valid_idempotency_key_and_valid_claim_return_the_parsed_Guid()
    {
        // Arrange
        var keyValue = "11111111-1111-1111-1111-111111111111";
        var claimValue = "22222222-2222-2222-2222-222222222222";
        var principalWithValidClaim = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(NameIdentifierClaim, claimValue) }));

        // Act
        var parsedKey = ShoppingCartEndpointApplicationBuilderExtensions.ParseIdempotencyKey(keyValue);
        var clientId = ShoppingCartEndpointApplicationBuilderExtensions.GetClientId(principalWithValidClaim);

        // Assert — guards do not over-reject valid input
        parsedKey.Should().Be(Guid.Parse(keyValue));
        clientId.Should().Be(Guid.Parse(claimValue));
    }
}
