using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Modules.Invoicing.API.Controllers;

[ApiController]
[Route("api/invoicing/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { module = "Invoicing", status = "Healthy" });
}
