using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Features.Auth.RefreshToken;

namespace WalletSystem.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthRefreshTokenController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthRefreshTokenController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }
}
