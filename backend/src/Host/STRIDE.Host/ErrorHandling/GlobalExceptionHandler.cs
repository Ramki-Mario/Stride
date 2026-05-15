using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Host.ErrorHandling;

/// <summary>
/// Global exception handler (registered via <c>app.AddExceptionHandler</c>).
/// Converts unhandled exceptions into RFC 7807 Problem Details responses.
///
/// Handled cases:
///   <see cref="ValidationException"/> (FluentValidation) → 400 with per-field errors
///   All other exceptions                                  → 500 (detail hidden in production)
///
/// <c>Result.Failure</c> is NOT an exception — controllers handle it explicitly
/// by checking <c>result.IsFailure</c> and returning appropriate HTTP status codes.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext       httpContext,
        Exception         exception,
        CancellationToken ct)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Type   = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title  = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problem, ct);
            return true;
        }

        // Log unhandled exceptions with full detail; return a safe 500.
        _logger.LogError(exception,
            "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var serverError = new ProblemDetails
        {
            Type   = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title  = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(serverError, ct);
        return true;
    }
}
