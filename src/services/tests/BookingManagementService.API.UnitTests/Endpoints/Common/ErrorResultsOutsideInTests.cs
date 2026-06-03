using System.Text.Json;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Domain.Error;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CinemaTicketBooking.Api.UnitTests.Endpoints.Common;

// Acceptance gate for slice 0003_assign_client_cart_result_http (ADR-002 step 3, first conversion).
// This slice has no WebApplicationFactory harness (none exists in the repo); per the PRD the gate
// is a focused unit spec of the new shared Error -> IResult mapper (ErrorResults), the Result-side
// analogue of CustomExceptionHandler. It executes the mapper's returned IResult against a
// DefaultHttpContext and asserts status + ProblemDetails body shape parity with CustomExceptionHandler:
// NotFoundError => 404 (+ Detail), ConflictError => 409 (title-only), any other Error => 500.
// RED until ErrorResults.ToProblem exists.
public class ErrorResultsOutsideInTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

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
    public async Task NotFoundError_Should_MapTo_404_With_ProblemDetails()
    {
        // Arrange
        var httpContext = CreateHttpContextWithBuffer();
        var error = new NotFoundError("ShoppingCart.NotFound", "Shopping cart not found");

        // Act
        await ErrorResults.ToProblem(error).ExecuteAsync(httpContext);

        // Assert — status
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        // Assert — ProblemDetails body (shape parity with CustomExceptionHandler's 404)
        var problem = await ReadProblemDetailsAsync(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.4");
        problem.Title.Should().Be("The specified resource was not found.");
        problem.Detail.Should().Be(error.Description);
    }

    [Fact]
    public async Task ConflictError_Should_MapTo_409_With_ProblemDetails()
    {
        // Arrange
        var httpContext = CreateHttpContextWithBuffer();
        var error = new ConflictError("ShoppingCart.ConflictException", "Active Shopping cart already exists");

        // Act
        await ErrorResults.ToProblem(error).ExecuteAsync(httpContext);

        // Assert — status
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        // Assert — ProblemDetails body (shape parity with CustomExceptionHandler's 409: title only)
        var problem = await ReadProblemDetailsAsync(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.8");
        problem.Title.Should().Be("Conflict");
        problem.Detail.Should().BeNull();
    }

    [Fact]
    public async Task UnrecognisedError_Should_MapTo_500_With_ProblemDetails()
    {
        // Arrange
        var httpContext = CreateHttpContextWithBuffer();
        var error = new Error("Some.Unmapped", "boom");

        // Act
        await ErrorResults.ToProblem(error).ExecuteAsync(httpContext);

        // Assert — status (preserves the former bare-throw collapse-to-500 behaviour)
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        // Assert — ProblemDetails body (shape parity with CustomExceptionHandler's 500: title only)
        var problem = await ReadProblemDetailsAsync(httpContext);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.6.1");
        problem.Title.Should().Be("Internal Server Error");
    }
}
