using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Transactions.Deposit;

namespace WalletSystem.Api.Controllers.Transactions;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsDepositController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsDepositController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Deposit funds into a wallet
    /// </summary>
    [HttpPost("deposit")]
    public async Task<ActionResult<TransactionResponse>> Deposit([FromBody] DepositCommand command, CancellationToken cancellationToken)
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
