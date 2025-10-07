using Microsoft.AspNetCore.Mvc;
using DotNetApp.Core.Abstractions;
using DotNetApp.Server.Contracts;

namespace DotNetApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StateController : ControllerBase
{
    private readonly IHealthService _healthService;

    public StateController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var status = await _healthService.GetStatusAsync(cancellationToken);
        var dto = new HealthDto { Status = status };
        return Ok(dto);
    }
}
