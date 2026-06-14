using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Administration.Application.Queries.GetAuditLog;

namespace STRIDE.Modules.Administration.API.Controllers;

/// <summary>
/// Tenant audit log — requires the tenant.settings permission.
/// GET /api/administration/audit-log
/// </summary>
[ApiController]
// Dynamic permission policy (US-134). "tenant.settings" is a key in the Identity
// permission catalog (DefaultPermissions); the TenantAdmin system role holds it.
[Authorize(Policy = "tenant.settings")]
[Route("api/administration/audit-log")]
public sealed class AuditLogController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ITenantContext _tenantContext;

    public AuditLogController(IMediator mediator, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Paginated, filterable audit log for the current tenant.
    /// GET /api/administration/audit-log?page=1&pageSize=50&from=&to=&action=
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int      page     = 1,
        [FromQuery] int      pageSize = 50,
        [FromQuery] DateTime? from    = null,
        [FromQuery] DateTime? to      = null,
        [FromQuery] string?  action   = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogQuery(
                TenantId: _tenantContext.TenantId,
                Page:     page,
                PageSize: pageSize,
                From:     from,
                To:       to,
                Action:   action),
            cancellationToken);

        return Ok(result);
    }
}
