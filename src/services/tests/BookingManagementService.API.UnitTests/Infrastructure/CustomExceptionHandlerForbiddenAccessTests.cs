using System.Text.Json;
using CinemaTicketBooking.Api.Infrastructure;
using CinemaTicketBooking.Application.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.UnitTests.Infrastructure;

// Central-mapping pin for slice 0009_cart_ownership_authorization (ADR-003 slice 1). Mirrors
// CustomExceptionHandlerInputGuardsTests (0008): it drives the real CustomExceptionHandler against a
// DefaultHttpContext and pins ForbiddenAccessException => 403 (ProblemDetails, "Forbidden",
// rfc7231 §6.5.3). The mapping is not changed by the slice; CartOwnershipBehaviour throws this
// exception and relies on this arm, so the test pins the contract the behaviour now depends on.
public class CustomExceptionHandlerForbiddenAccessTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static CustomExceptionHandler CreateHandler()
        => new(Substitute.For<ILogger>());

    private static HttpContext CreateHttpContextWithBuffer()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<T?> ReadBodyAsync<T>(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(context.Response.Body, WebJson);
    }

    [Fact]
    public async Task ForbiddenAccessException_Should_MapTo_403_With_ProblemDetails()
    {
        // Arrange
        var handler = CreateHandler();
        var httpContext = CreateHttpContextWithBuffer();
        var exception = new ForbiddenAccessException();

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert — status
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        // Assert — ProblemDetails body shape
        var problem = await ReadBodyAsync<ProblemDetails>(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status403Forbidden);
        problem.Title.Should().Be("Forbidden");
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.3");
    }
}
