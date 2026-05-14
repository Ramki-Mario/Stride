using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Modules.Administration.API.Controllers;

[ApiController]
[Route("api/administration/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { module = "Administration", status = "Healthy" });
}
