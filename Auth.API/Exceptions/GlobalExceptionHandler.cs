using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unhandled exception occurred.");

        var statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,

            KeyNotFoundException => StatusCodes.Status404NotFound,

            UnauthorizedAccessException =>
                StatusCodes.Status401Unauthorized,

            InvalidOperationException =>
                StatusCodes.Status409Conflict,

            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest =>
                "Bad Request",

            StatusCodes.Status401Unauthorized =>
                "Unauthorized",

            StatusCodes.Status404NotFound =>
                "Not Found",

            StatusCodes.Status409Conflict =>
                "Conflict",

            _ => "An unexpected error occurred."
        };
    }
}