using MediatR;
using WalletSystem.Application.Features.Auth.Login;
using WalletSystem.Application.Features.Auth.RefreshToken;
using WalletSystem.Application.Features.Users.GetCurrentUser;
using WalletSystem.Application.Features.Users.Register;

namespace WalletSystem.Api.Endpoints;

/// <summary>
/// Authentication endpoints: /api/auth/*
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // POST /api/auth/register - Register a new user account
        group.MapPost("/register", async (RegisterUserCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        })
        .AllowAnonymous()
        .Produces<RegisterUserResponse>()
        .Produces(400)
        .WithSummary("Register a new user account");

        // POST /api/auth/login - Login and receive JWT token
        group.MapPost("/login", async (LoginCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Results.Unauthorized();
            }
        })
        .AllowAnonymous()
        .Produces<LoginResponse>()
        .Produces(401)
        .WithSummary("Login and receive JWT token");

        // POST /api/auth/refresh - Refresh access token using refresh token
        group.MapPost("/refresh", async (RefreshTokenCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Results.Unauthorized();
            }
        })
        .AllowAnonymous()
        .Produces<RefreshTokenResponse>()
        .Produces(401)
        .WithSummary("Refresh access token using refresh token");

        // GET /api/auth/me - Get current authenticated user information
        group.MapGet("/me", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(new GetCurrentUserQuery(), cancellationToken);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .Produces<GetCurrentUserResponse>()
        .Produces(401)
        .Produces(404)
        .WithSummary("Get current authenticated user information");

        return app;
    }
}
