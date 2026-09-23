using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Transactions.Deposit;
using WalletSystem.Application.Features.Transactions.Withdraw;

namespace WalletSystem.Api.Controllers.Transactions;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsWithdrawController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsWithdrawController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Withdraw funds from a wallet
    /// </summary>
    [HttpPost("withdraw")]
    public async Task<ActionResult<TransactionResponse>> Withdraw([FromBody] WithdrawCommand command, CancellationToken cancellationToken)
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
