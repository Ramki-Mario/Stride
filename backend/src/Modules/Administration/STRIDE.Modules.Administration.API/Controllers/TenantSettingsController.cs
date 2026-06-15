using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Administration.API.DTOs;
using STRIDE.Modules.Administration.Application.Commands.CompleteOnboarding;
using STRIDE.Modules.Administration.Application.Commands.UpdateTenantSettings;
using STRIDE.Modules.Administration.Application.Queries.GetTenantSettings;
using STRIDE.Modules.Administration.Infrastructure.Services;

namespace STRIDE.Modules.Administration.API.Controllers;

/// <summary>
/// Tenant settings endpoints.
///
///   GET  /api/administration/settings              — get current tenant settings
///   PUT  /api/administration/settings              — update tenant settings (+ CSS sanitisation)
///   GET  /api/administration/settings/css-template — download --stride-* token template file
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

    /// <summary>GET /api/administration/settings</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetTenantSettingsQuery(_tenantContext.TenantId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// PUT /api/administration/settings — updates tenant settings.
    /// If <c>customCss</c> is provided, the CSS is sanitised and only valid
    /// <c>--stride-*</c> tokens are stored. Returns the sanitisation report
    /// (200 OK) so the caller can surface per-line feedback; returns 204 NoContent
    /// when no CSS was submitted.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateTenantSettingsRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateTenantSettingsCommand(
            _tenantContext.TenantId,
            body.DisplayName,
            body.DefaultPalette,
            body.Timezone,
            body.CustomCss,
            _currentUser.UserId,
            body.EnabledModules), cancellationToken);

        if (result.IsFailure)
            return Problem(result.Error);

        // If CSS was included, return the sanitisation report.
        return result.Value is not null
            ? Ok(result.Value)
            : NoContent();
    }

    /// <summary>POST /api/administration/settings/complete-onboarding — marks wizard complete.</summary>
    [HttpPost("complete-onboarding")]
    public async Task<IActionResult> CompleteOnboarding(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteOnboardingCommand(_tenantContext.TenantId, _currentUser.UserId),
            cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    /// <summary>
    /// GET /api/administration/settings/css-template — returns a ready-to-edit CSS
    /// file listing every supported <c>--stride-*</c> token with its default value.
    /// </summary>
    [HttpGet("css-template")]
    public IActionResult GetCssTemplate()
    {
        var css = CssTemplateProvider.GetTemplate();
        return File(
            System.Text.Encoding.UTF8.GetBytes(css),
            "text/css",
            "stride-theme-template.css");
    }
}
