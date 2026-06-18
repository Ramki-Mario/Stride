using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for the platform-admin surface. Only users with the "superadmin" cookie
/// claim (stored in the Redis session) may call these endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("bff/platform")]
public sealed class PlatformAdminController : ControllerBase
{
    private readonly PlatformApiClient           _platform;
    private readonly ILogger<PlatformAdminController> _logger;

    public PlatformAdminController(PlatformApiClient platform, ILogger<PlatformAdminController> logger)
    {
        _platform = platform;
        _logger   = logger;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin()) return Forbid();

        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _platform.GetTenantsAsync(token, cancellationToken);
        var body     = await response.Content.ReadAsStringAsync(cancellationToken);
        return StatusCode((int)response.StatusCode, body);
    }

    [HttpGet("tenants/{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id, CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin()) return Forbid();

        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _platform.GetTenantAsync(id, token, cancellationToken);
        var body     = await response.Content.ReadAsStringAsync(cancellationToken);
        return StatusCode((int)response.StatusCode, body);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin()) return Forbid();

        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _platform.GetStatsAsync(token, cancellationToken);
        var body     = await response.Content.ReadAsStringAsync(cancellationToken);
        return StatusCode((int)response.StatusCode, body);
    }

    private bool IsSuperAdmin() =>
        HttpContext.User.FindFirst("superadmin")?.Value == "true";
}
