using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Features.Auth.Login;

namespace WalletSystem.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthLoginController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthLoginController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Login and receive JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
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
