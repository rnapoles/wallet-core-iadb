using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Wallets;
using WalletSystem.Application.Features.Wallets.GetWalletsByUserId;

namespace WalletSystem.Api.Controllers.Wallets;

[ApiController]
[Route("api/wallets")]
[Authorize]
public class WalletsGetUserWalletsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletsGetUserWalletsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all wallets for the authenticated user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WalletDto>>> GetUserWallets(CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetWalletsByUserIdQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
