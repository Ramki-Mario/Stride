using Microsoft.AspNetCore.Mvc;

namespace STRIDE.Modules.Clients.API.Controllers;

[ApiController]
[Route("api/clients/health")]
public sealed class ClientsHealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { module = "Clients", status = "healthy" });
}
