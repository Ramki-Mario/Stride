using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Modules.Notifications.API.Controllers;

[ApiController]
[Route("api/notifications/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { module = "Notifications", status = "Healthy" });
}
