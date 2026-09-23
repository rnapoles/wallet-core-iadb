using Microsoft.AspNetCore.Mvc;

namespace WalletSystem.Api.Controllers.Health;

[ApiController]
[Route("api/health")]
public class HealthLiveController : ControllerBase
{
    /// <summary>
    /// Quick health check endpoint for load balancers
    /// </summary>
    [HttpGet("live")]
    public IActionResult Liveness()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}
