using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Features.Users.GetCurrentUser;

namespace WalletSystem.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthMeController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthMeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<GetCurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
