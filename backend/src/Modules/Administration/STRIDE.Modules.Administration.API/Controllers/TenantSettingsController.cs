using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Administration.API.DTOs;
using STRIDE.Modules.Administration.Application.Commands.UpdateTenantSettings;
using STRIDE.Modules.Administration.Application.Queries.GetTenantSettings;

namespace STRIDE.Modules.Administration.API.Controllers;

/// <summary>
/// Tenant settings endpoints.
///
///   GET  /api/administration/settings  — get current tenant settings
///   PUT  /api/administration/settings  — update tenant settings
/// </summary>
[ApiController]
[Authorize]
[Route("api/administration/settings")]
public sealed class TenantSettingsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public TenantSettingsController(
        IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>GET /api/administration/settings — returns current tenant settings.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetTenantSettingsQuery(_tenantContext.TenantId), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>PUT /api/administration/settings — update tenant settings.</summary>
    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateTenantSettingsRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateTenantSettingsCommand(
            _tenantContext.TenantId,
            body.DisplayName,
            body.DefaultPalette,
            body.Timezone,
            body.CustomCssTokensJson,
            _currentUser.UserId), ct);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}
