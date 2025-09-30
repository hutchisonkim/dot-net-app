using Microsoft.AspNetCore.Mvc;

namespace DotNetApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StateController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy" });
    }
}
