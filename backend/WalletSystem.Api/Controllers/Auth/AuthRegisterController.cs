using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Features.Users.Register;

namespace WalletSystem.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthRegisterController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthRegisterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterUserResponse>> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
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
