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

        var statusCode = exception switch
        {
            BadHttpRequestException badRequestEx => badRequestEx.StatusCode,
            ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status500InternalServerError
        };

        if (exception != null)
        {
            Log.Error(exception,
                "Unhandled exception occurred. RequestId: {RequestId}, Method: {Method}, Endpoint: {Endpoint}, Path: {Path}, Details: {ExceptionDetails}",
                requestId, method, endpoint, path, exception.ToString());
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Checks both common variables used by .NET hosting
        string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        var message = "An unexpected error occurred.";
        if (environment == "Testing" || environment == "Test")
        {
            message = exception?.Message ?? message;
        }
        
        var response = new
        {
            success = false,
            code = statusCode,
            //message = "An unexpected error occurred." // Never report API Information
              message = message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}

