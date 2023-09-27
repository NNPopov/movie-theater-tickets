using System.Collections.Frozen;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Api.Infrastructure;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly FrozenDictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;
    private readonly ILogger _logger;

    public CustomExceptionHandler(ILogger logger)
    {
        this._logger = logger;

        Dictionary<Type, Func<HttpContext, Exception, Task>> exceptionHandlers
            = new Dictionary<Type, Func<HttpContext, Exception, Task>>();

        exceptionHandlers.Add(typeof(Exception), HandleException);
        exceptionHandlers.Add(typeof(ValidationException), HandleValidationException);
        exceptionHandlers.Add(typeof(NotFoundException), HandleNotFoundException);
        exceptionHandlers.Add(typeof(ContentNotFoundException), HandleContentNotFoundException);
        exceptionHandlers.Add(typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException);
        exceptionHandlers.Add(typeof(ForbiddenAccessException), HandleForbiddenAccessException);
        exceptionHandlers.Add(typeof(ConflictException), HandleConflictExceptionException);
        exceptionHandlers.Add(typeof(DuplicateRequestException), HandleDuplicateRequestExceptionException);
        exceptionHandlers.Add(typeof(LockedException), HandleLockedExceptionExceptionException);
        
        
        _exceptionHandlers = FrozenDictionary.ToFrozenDictionary(exceptionHandlers);
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.TryGetValue(exceptionType, out var handler))
        {
            await handler.Invoke(httpContext, exception);
            return true;
        }

        await HandleException(httpContext, exception);

        return true;
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        _logger.Error(ex, "Internal Server Error");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
        });
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        _logger.Error(ex, "Validation Exception");

        var exception = (ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    {
        _logger.Warning(ex, "Not Found");

        var exception = (NotFoundException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = exception.Message
        });
    }

    private async Task HandleContentNotFoundException(HttpContext httpContext, Exception ex)
    {
        _logger.Warning(ex.Message);

        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;

        await httpContext.Response.CompleteAsync();
    }


    private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    {
        _logger.Warning(ex, "Unauthorized Access");

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });
    }

    private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    {
        _logger.Error(ex, "Forbidden Access");

        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        });
    }
    
    private async Task HandleConflictExceptionException(HttpContext httpContext, Exception ex)
    {
        _logger.Error(ex, "Conflict Exception");

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
        });
    }
    

    
    private async Task HandleDuplicateRequestExceptionException(HttpContext httpContext, Exception ex)
    {
        _logger.Error(ex, "Duplicate Request");

        httpContext.Response.StatusCode = StatusCodes.Status200OK;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status200OK,
            Title = "DuplicateRequest",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.3.1"
        });
    }
    
    private async Task HandleLockedExceptionExceptionException(HttpContext httpContext, Exception ex)
    {
        _logger.Error(ex, "Object Locked");

        httpContext.Response.StatusCode = StatusCodes.Status423Locked;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status423Locked,
            Title = "Locked",
            //Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.3.1"
        });
    }
    
}