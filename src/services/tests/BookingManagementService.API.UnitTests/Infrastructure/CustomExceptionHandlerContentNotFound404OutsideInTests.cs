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

// Acceptance gate for slice 0002_content_not_found_404 (ADR-002 step 2).
// This slice has no WebApplicationFactory harness (none exists in the repo); per the PRD the
// gate is a focused unit test of the central translation point. It drives the real
// CustomExceptionHandler against a DefaultHttpContext and asserts that ContentNotFoundException
// now maps to 404 + ProblemDetails (RED until the writer is flipped), while NotFoundException
// still maps to 404 (regression — green before and after).
public class CustomExceptionHandlerContentNotFound404OutsideInTests
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

    private static async Task<ProblemDetails?> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, WebJson);
    }

    [Fact]
    public async Task ContentNotFoundException_Should_MapTo_404_With_ProblemDetails()
    {
        // Arrange
        var handler = CreateHandler();
        var httpContext = CreateHttpContextWithBuffer();
        var exception = new ContentNotFoundException("00000000-0000-0000-0000-000000000000", "Movie");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert — status
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        // Assert — ProblemDetails body
        var problem = await ReadProblemDetailsAsync(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.4");
        problem.Title.Should().Be("The specified resource was not found.");
        problem.Detail.Should().Be(exception.Message);
    }

    [Fact]
    public async Task NotFoundException_Should_Still_MapTo_404_With_ProblemDetails()
    {
        // Arrange
        var handler = CreateHandler();
        var httpContext = CreateHttpContextWithBuffer();
        var exception = new NotFoundException("00000000-0000-0000-0000-000000000000", "ShoppingCart");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert — status (regression: unchanged)
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        // Assert — ProblemDetails body shape (parity with ContentNotFoundException)
        var problem = await ReadProblemDetailsAsync(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.4");
        problem.Title.Should().Be("The specified resource was not found.");
        problem.Detail.Should().Be(exception.Message);
    }
}
