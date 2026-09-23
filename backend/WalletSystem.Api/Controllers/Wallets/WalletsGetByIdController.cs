using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Wallets;
using WalletSystem.Application.Features.Wallets.GetWalletById;

namespace WalletSystem.Api.Controllers.Wallets;

[ApiController]
[Route("api/wallets")]
[Authorize]
public class WalletsGetByIdController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletsGetByIdController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get a specific wallet by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WalletDto>> GetWallet(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetWalletByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
