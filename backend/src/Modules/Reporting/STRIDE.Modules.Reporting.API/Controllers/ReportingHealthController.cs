using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Modules.Reporting.API.Controllers;

[ApiController]
[Route("api/reporting/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { module = "Reporting", status = "Healthy" });
}
