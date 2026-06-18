using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.API.Controllers;

/// <summary>
/// Platform-admin endpoints — accessible only to users whose JWT carries the
/// "superadmin" claim (set on login for emails in the SuperAdmins table).
///
/// These endpoints bypass per-tenant isolation: they return data across all tenants.
/// </summary>
[ApiController]
[Authorize]
[Route("api/platform")]
public sealed class PlatformAdminController : ControllerBase
{
    private readonly IPlatformQueryService _platform;

    public PlatformAdminController(IPlatformQueryService platform) => _platform = platform;

    /// <summary>GET /api/platform/tenants — all tenants with seat count and last activity.</summary>
    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin()) return Forbid();
        var tenants = await _platform.GetAllTenantsAsync(cancellationToken);
        return Ok(tenants);
    }

    /// <summary>GET /api/platform/tenants/{id} — detail view for one tenant.</summary>
    [HttpGet("tenants/{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id, CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin()) return Forbid();
        var tenant = await _platform.GetTenantDetailAsync(id, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    /// <summary>GET /api/platform/stats — platform-wide counts.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin()) return Forbid();
        var stats = await _platform.GetStatsAsync(cancellationToken);
        return Ok(stats);
    }

    private bool IsSuperAdmin() =>
        HttpContext.User.FindFirst("superadmin")?.Value == "true";
}
