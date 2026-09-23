using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Transactions.Deposit;
using WalletSystem.Application.Features.Transactions.Transfer;

namespace WalletSystem.Api.Controllers.Transactions;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsTransferController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsTransferController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Transfer funds between wallets
    /// </summary>
    [HttpPost("transfer")]
    public async Task<ActionResult<TransactionResponse>> Transfer([FromBody] TransferCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
