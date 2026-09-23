using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace WalletSystem.Api.Common.Middleware;

public static class GlobalExceptionHandler
{
    public static async Task HandleExceptionAsync(HttpContext context)
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>()
            ?? context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? context.TraceIdentifier;
        var method = context.Request.Method;
        var path = exceptionFeature?.Path ?? context.Request.Path.Value ?? "Unknown";
        var endpoint = exceptionFeature?.Endpoint?.DisplayName ?? path;

        // Client-error class exceptions carry safe, client-facing messages (they are thrown
        // by our own code with intentional text). Unknown/unexpected exceptions (5xx) must
        // never leak internal details to the API consumer.
        var statusCode = exception switch
        {
            BadHttpRequestException badRequestEx => badRequestEx.StatusCode,
            ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status500InternalServerError
        };

        var message = exception is not null
            ? exception.Message // Exception types reaching this handler are safe, client-facing API messages
            : "An unexpected error occurred.";

        if (exception != null)
        {
            Log.Error(exception,
                "Unhandled exception occurred. RequestId: {RequestId}, Method: {Method}, Endpoint: {Endpoint}, Path: {Path}, Details: {ExceptionDetails}",
                requestId, method, endpoint, path, exception.ToString());
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            code = statusCode,
            message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}

