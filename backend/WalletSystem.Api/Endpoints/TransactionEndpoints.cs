using MediatR;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Transactions.Deposit;
using WalletSystem.Application.Features.Transactions.GetTransactionById;
using WalletSystem.Application.Features.Transactions.GetTransactionsByWalletId;
using WalletSystem.Application.Features.Transactions.Transfer;
using WalletSystem.Application.Features.Transactions.Withdraw;

namespace WalletSystem.Api.Endpoints;

/// <summary>
/// Transaction endpoints: /api/transactions/* (all require authentication)
/// </summary>
public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions")
                       .WithTags("Transactions")
                       .RequireAuthorization();

        // POST /api/transactions/deposit - Deposit funds into a wallet
        group.MapPost("/deposit", async (DepositCommand command, IMediator mediator, CancellationToken cancellationToken) =>
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
        .Produces<TransactionResponse>()
        .Produces(400)
        .WithSummary("Deposit funds into a wallet");

        // POST /api/transactions/withdraw - Withdraw funds from a wallet
        group.MapPost("/withdraw", async (WithdrawCommand command, IMediator mediator, CancellationToken cancellationToken) =>
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
        .Produces<TransactionResponse>()
        .Produces(400)
        .WithSummary("Withdraw funds from a wallet");

        // POST /api/transactions/transfer - Transfer funds between wallets
        group.MapPost("/transfer", async (TransferCommand command, IMediator mediator, CancellationToken cancellationToken) =>
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
        .Produces<TransactionResponse>()
        .Produces(400)
        .WithSummary("Transfer funds between wallets");

        // GET /api/transactions/{id} - Get a specific transaction by ID
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(new GetTransactionByIdQuery(id), cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        })
        .Produces<TransactionDto>()
        .Produces(404)
        .WithSummary("Get a specific transaction by ID");

        // GET /api/transactions/wallet/{walletId} - Get transaction history for a wallet
        group.MapGet("/wallet/{walletId:guid}", async (Guid walletId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(new GetTransactionsByWalletIdQuery(walletId), cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        })
        .Produces<IEnumerable<TransactionDto>>()
        .Produces(400)
        .WithSummary("Get transaction history for a wallet");

        return app;
    }
}
