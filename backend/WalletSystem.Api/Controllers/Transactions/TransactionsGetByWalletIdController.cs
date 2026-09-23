using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Transactions.GetTransactionsByWalletId;

namespace WalletSystem.Api.Controllers.Transactions;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsGetByWalletIdController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsGetByWalletIdController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get transaction history for a wallet
    /// </summary>
    [HttpGet("wallet/{walletId:guid}")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetWalletTransactions(Guid walletId, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetTransactionsByWalletIdQuery(walletId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
