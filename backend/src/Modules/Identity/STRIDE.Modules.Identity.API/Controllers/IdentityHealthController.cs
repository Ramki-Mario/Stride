using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Modules.Identity.API.Controllers;

[ApiController]
[Route("api/identity/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { module = "Identity", status = "Healthy" });
}
