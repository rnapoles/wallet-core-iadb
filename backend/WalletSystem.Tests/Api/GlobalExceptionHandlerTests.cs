using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using WalletSystem.Api.Common.Middleware;
using Xunit;

namespace WalletSystem.Tests.Api;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task HandleExceptionAsync_WithInvalidOperationException_ReturnsBadRequest400()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/wallets";
        context.Request.Method = "POST";
        context.TraceIdentifier = "req-123";

        var exception = new InvalidOperationException("Wallet already exists");
        var feature = new ExceptionHandlerFeature
        {
            Error = exception,
            Path = "/api/wallets"
        };
        context.Features.Set<IExceptionHandlerFeature>(feature);
        context.Features.Set<IExceptionHandlerPathFeature>(feature);

        // Act
        await GlobalExceptionHandler.HandleExceptionAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(StatusCodes.Status400BadRequest, root.GetProperty("code").GetInt32());
        Assert.Equal("Wallet already exists", root.GetProperty("message").GetString());
    }

    [Fact]
    public async Task HandleExceptionAsync_WithUnauthorizedAccessException_ReturnsUnauthorized401()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/auth/me";
        context.Request.Method = "GET";

        var exception = new UnauthorizedAccessException("Unauthorized access token");
        var feature = new ExceptionHandlerFeature
        {
            Error = exception,
            Path = "/api/auth/me"
        };
        context.Features.Set<IExceptionHandlerFeature>(feature);
        context.Features.Set<IExceptionHandlerPathFeature>(feature);

        // Act
        await GlobalExceptionHandler.HandleExceptionAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(StatusCodes.Status401Unauthorized, root.GetProperty("code").GetInt32());
        Assert.Equal("Unauthorized access token", root.GetProperty("message").GetString());
    }

    [Fact]
    public async Task HandleExceptionAsync_WithKeyNotFoundException_ReturnsNotFound404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/wallets/123";
        context.Request.Method = "GET";

        var exception = new KeyNotFoundException("Wallet not found");
        var feature = new ExceptionHandlerFeature
        {
            Error = exception,
            Path = "/api/wallets/123"
        };
        context.Features.Set<IExceptionHandlerFeature>(feature);
        context.Features.Set<IExceptionHandlerPathFeature>(feature);

        // Act
        await GlobalExceptionHandler.HandleExceptionAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(StatusCodes.Status404NotFound, root.GetProperty("code").GetInt32());
        Assert.Equal("Wallet not found", root.GetProperty("message").GetString());
    }

    [Fact]
    public async Task HandleExceptionAsync_WithGenericException_ReturnsInternalServerError500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/transactions";
        context.Request.Method = "POST";
        context.Request.Headers["X-Request-ID"] = "custom-req-id-999";

        var exception = new Exception("Database connection failure");
        var feature = new ExceptionHandlerFeature
        {
            Error = exception,
            Path = "/api/transactions"
        };
        context.Features.Set<IExceptionHandlerFeature>(feature);
        context.Features.Set<IExceptionHandlerPathFeature>(feature);

        // Act
        await GlobalExceptionHandler.HandleExceptionAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(StatusCodes.Status500InternalServerError, root.GetProperty("code").GetInt32());
        Assert.Equal("Database connection failure", root.GetProperty("message").GetString());
    }
}

