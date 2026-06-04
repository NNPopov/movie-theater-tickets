using System.Text.Json;
using CinemaTicketBooking.Api.Infrastructure;
using CinemaTicketBooking.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.UnitTests.Infrastructure;

// Central-mapping characterization for slice 0008_endpoint_input_guards (ADR-002 endpoint-helper tail).
// Mirrors CustomExceptionHandlerContentNotFound404OutsideInTests (0002): it drives the real
// CustomExceptionHandler against a DefaultHttpContext and pins the two status contracts the slice's
// guards rely on — DomainValidationException => 400 (ValidationProblemDetails, rfc7231 §6.5.1) and
// UnauthorizedAccessException => 401 (ProblemDetails, "Unauthorized", rfc7235 §3.1). These mappings
// are not changed by the slice; the test pins them because no dedicated test covered them before.
public class CustomExceptionHandlerInputGuardsTests
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
    public async Task DomainValidationException_Should_MapTo_400_With_ValidationProblemDetails()
    {
        // Arrange
        var handler = CreateHandler();
        var httpContext = CreateHttpContextWithBuffer();
        var exception = new DomainValidationException("Invalid idempotency key: not-a-guid");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert — status
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        // Assert — ValidationProblemDetails body shape
        var problem = await ReadBodyAsync<ValidationProblemDetails>(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        problem.Errors.Should().ContainKey(exception.Message);
    }

    [Fact]
    public async Task UnauthorizedAccessException_Should_MapTo_401_With_ProblemDetails()
    {
        // Arrange
        var handler = CreateHandler();
        var httpContext = CreateHttpContextWithBuffer();
        var exception = new UnauthorizedAccessException("Invalid nameidentifier claim: not-a-guid");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert — status
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        // Assert — ProblemDetails body shape
        var problem = await ReadBodyAsync<ProblemDetails>(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problem.Title.Should().Be("Unauthorized");
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7235#section-3.1");
    }
}
