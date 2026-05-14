using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Modules.Workflows.API.Controllers;

[ApiController]
[Route("api/workflows/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { module = "Workflows", status = "Healthy" });
}
