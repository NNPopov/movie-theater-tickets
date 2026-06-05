using CinemaTicketBooking.Domain.Error;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Endpoints.Common;

// Shared Result-side analogue of CustomExceptionHandler: translates a failing Result's Error
// into an HTTP IResult whose ProblemDetails body shape is identical to what CustomExceptionHandler
// emits for the same status. This is the single Error -> IResult policy point reused by every
// ADR-002 step-3 conversion (see slice 0003_assign_client_cart_result_http).
//
// The body is written with Response.WriteAsJsonAsync (the same primitive CustomExceptionHandler
// uses), so the result executes without requiring IProblemDetailsService from RequestServices.
public static class ErrorResults
{
    public static IResult ToProblem(Error error) => error switch
    {
        NotFoundError => Problem(
            StatusCodes.Status404NotFound,
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            title: "The specified resource was not found.",
            detail: error.Description),
        ConflictError => Problem(
            StatusCodes.Status409Conflict,
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            title: "Conflict"),
        _ => Problem(
            StatusCodes.Status500InternalServerError,
            type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title: "Internal Server Error"),
    };

    private static IResult Problem(int statusCode, string type, string title, string? detail = null) =>
        new ProblemDetailsResult(new ProblemDetails
        {
            Status = statusCode,
            Type = type,
            Title = title,
            Detail = detail,
        });

    private sealed class ProblemDetailsResult(ProblemDetails problemDetails) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            return httpContext.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
