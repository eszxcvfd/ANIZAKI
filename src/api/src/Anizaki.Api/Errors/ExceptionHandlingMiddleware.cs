using Anizaki.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System;
using System.Threading.Tasks;

namespace Anizaki.Api.Errors;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        var correlationId = CorrelationIdResolver.Resolve(httpContext);

        if (exception is RequestThrottledException throttledException)
        {
            _logger.LogWarning("Request throttled: {Message}", throttledException.Message);

            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (throttledException.RetryAfterUtc is not null)
            {
                var remainingSeconds = Math.Max(
                    1,
                    (int)Math.Ceiling((throttledException.RetryAfterUtc.Value - DateTime.UtcNow).TotalSeconds));
                httpContext.Response.Headers.RetryAfter = remainingSeconds.ToString(CultureInfo.InvariantCulture);
            }

            await httpContext.Response.WriteAsJsonAsync(new ApiErrorEnvelope(
                Error: "too_many_requests",
                Message: throttledException.Message,
                CorrelationId: correlationId));

            return;
        }

        if (exception is RequestValidationException validationException)
        {
            _logger.LogWarning("Request validation failed (ValidationErrors details not logged directly as they are returned to client).");

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new ApiErrorEnvelope(
                Error: "validation_failed",
                Message: "Request validation failed.",
                CorrelationId: correlationId,
                Errors: validationException.Errors));

            return;
        }

        _logger.LogError(exception, "An unexpected error occurred.");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ApiErrorEnvelope(
            Error: "internal_server_error",
            Message: "An unexpected error occurred.",
            CorrelationId: correlationId));
    }
}
