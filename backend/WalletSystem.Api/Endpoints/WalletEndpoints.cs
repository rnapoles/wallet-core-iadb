using MediatR;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Wallets.CreateWallet;
using WalletSystem.Application.Features.Wallets.GetWalletById;
using WalletSystem.Application.Features.Wallets.GetWalletsByUserId;

namespace WalletSystem.Api.Endpoints;

/// <summary>
/// Wallet endpoints: /api/wallets/* (all require authentication)
/// </summary>
public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wallets")
                       .WithTags("Wallets")
                       .RequireAuthorization();

        // POST /api/wallets - Create a new wallet for the authenticated user
        group.MapPost("/", async (CreateWalletCommand command, IMediator mediator, CancellationToken cancellationToken) =>
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
        .Produces<CreateWalletResponse>()
        .Produces(400)
        .WithSummary("Create a new wallet for the authenticated user");

        // GET /api/wallets - Get all wallets for the authenticated user
        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(new GetWalletsByUserIdQuery(), cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        })
        .Produces<IEnumerable<WalletDto>>()
        .Produces(400)
        .WithSummary("Get all wallets for the authenticated user");

        // GET /api/wallets/{id} - Get a specific wallet by ID
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(new GetWalletByIdQuery(id), cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        })
        .Produces<WalletDto>()
        .Produces(404)
        .WithSummary("Get a specific wallet by ID");

        return app;
    }
}
