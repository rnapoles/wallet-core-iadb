using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Transactions.GetTransactionById;

namespace WalletSystem.Api.Controllers.Transactions;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsGetByIdController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsGetByIdController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get a specific transaction by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetTransactionByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
