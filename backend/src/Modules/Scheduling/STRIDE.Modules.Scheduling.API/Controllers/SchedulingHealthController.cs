using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Modules.Scheduling.API.Controllers;

[ApiController]
[Route("api/scheduling/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { module = "Scheduling", status = "Healthy" });
}
