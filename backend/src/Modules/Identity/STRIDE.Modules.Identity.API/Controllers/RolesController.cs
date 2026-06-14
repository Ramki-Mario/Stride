using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.Modules.Identity.Application.Queries.ListRoles;
using STRIDE.Modules.Identity.Domain;

namespace STRIDE.Modules.Identity.API.Controllers;

/// <summary>
/// Read-only role catalogue endpoints.
/// GET /api/identity/roles — list all active roles for the current tenant.
/// </summary>
[ApiController]
[Authorize]
[Route("api/identity/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns all active roles for the current tenant, ordered by name.
    /// Used by the workflow builder to populate the "Required Role" dropdown per step.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = DefaultPermissions.RoleView)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListRolesQuery(), cancellationToken);
        return Ok(result.Value);
    }
}
