using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.Modules.Identity.API.Dtos;
using STRIDE.Modules.Identity.Application.Commands.RegisterTenant;

namespace STRIDE.Modules.Identity.API.Controllers;

/// <summary>
/// Unauthenticated tenant-lifecycle endpoints.
///
/// POST /api/identity/tenants/register
///   Self-service tenant registration. Creates a new org + first admin user in a
///   single atomic operation and returns a JWT (same shape as /auth/login).
///   The BFF exchanges this for an HttpOnly session cookie.
/// </summary>
[ApiController]
[Route("api/identity/tenants")]
[AllowAnonymous]
public sealed class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Register a new tenant workspace.
    /// Returns 200 + JWT on success (BFF converts to session cookie).
    /// Returns 400 on validation errors, 409 if the slug is already taken.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterTenantRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await _mediator.Send(new RegisterTenantCommand(
            OrgName:          request.OrgName,
            Slug:             request.Slug,
            Plan:             request.Plan,
            AdminEmail:       request.AdminEmail,
            AdminPassword:    request.AdminPassword,
            AdminDisplayName: request.AdminDisplayName), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("slug", StringComparison.OrdinalIgnoreCase) &&
                result.Error.Contains("taken", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
