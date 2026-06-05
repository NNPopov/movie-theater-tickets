using System.Security.Claims;
using CinemaTicketBooking.Api.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Api.UnitTests.Authentication;

// Unit gate for slice 0009: CurrentUser reads the same nameidentifier claim as GetClientId and
// returns anonymous (no throw) when the claim is missing or not a Guid (F10, F11, F12). The
// conditional ownership check — not the identity reader — is what decides the 403.
public class CurrentUserTests
{
    private const string NameIdentifier =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

    private static CurrentUser CreateSut(params Claim[] claims)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"))
        };
        accessor.HttpContext.Returns(httpContext);
        return new CurrentUser(accessor);
    }

    [Fact]
    public void Should_Be_Authenticated_With_Parsed_ClientId_When_NameIdentifier_Is_A_Valid_Guid()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var sut = CreateSut(new Claim(NameIdentifier, clientId.ToString()));

        // Act & Assert
        sut.IsAuthenticated.Should().BeTrue();
        sut.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void Should_Be_Anonymous_When_NameIdentifier_Claim_Is_Absent()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.IsAuthenticated.Should().BeFalse();
        sut.ClientId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Should_Be_Anonymous_When_NameIdentifier_Claim_Is_Not_A_Guid()
    {
        // Arrange
        var sut = CreateSut(new Claim(NameIdentifier, "not-a-guid"));

        // Act & Assert
        sut.IsAuthenticated.Should().BeFalse();
        sut.ClientId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Should_Be_Anonymous_When_There_Is_No_HttpContext()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var sut = new CurrentUser(accessor);

        // Act & Assert
        sut.IsAuthenticated.Should().BeFalse();
        sut.ClientId.Should().Be(Guid.Empty);
    }
}
