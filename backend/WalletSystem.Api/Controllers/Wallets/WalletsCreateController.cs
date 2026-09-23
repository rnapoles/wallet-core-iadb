using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Features.Wallets.CreateWallet;

namespace WalletSystem.Api.Controllers.Wallets;

[ApiController]
[Route("api/wallets")]
[Authorize]
public class WalletsCreateController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletsCreateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new wallet for the authenticated user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateWalletResponse>> CreateWallet([FromBody] CreateWalletCommand command, CancellationToken cancellationToken)
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
