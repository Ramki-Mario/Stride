using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.API.Dtos;
using STRIDE.Modules.Identity.Application.Commands.CreateRole;
using STRIDE.Modules.Identity.Application.Commands.DeleteRole;
using STRIDE.Modules.Identity.Application.Commands.UpdateRole;
using STRIDE.Modules.Identity.Application.Queries.GetRole;
using STRIDE.Modules.Identity.Application.Queries.ListRoles;
using STRIDE.Modules.Identity.Domain;

namespace STRIDE.Modules.Identity.API.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IMediator   _mediator;
    private readonly ICurrentUser _currentUser;

    public RolesController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator    = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns all active roles for the current tenant, ordered by name.
    /// Used by the workflow builder to populate the "Required Role" dropdown per step.
    /// GET /api/identity/roles
    /// </summary>
    [HttpGet]
    [Authorize(Policy = DefaultPermissions.RoleView)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListRolesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Returns a single role including its active permissions.
    /// GET /api/identity/roles/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = DefaultPermissions.RoleView)]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRoleQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new custom role with the specified permissions.
    /// Enforces a no-escalation guard — the caller cannot grant permissions they don't hold.
    /// POST /api/identity/roles
    /// </summary>
    [HttpPost]
    [Authorize(Policy = DefaultPermissions.RoleManage)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await _mediator.Send(
            new CreateRoleCommand(
                Name:          request.Name,
                Description:   request.Description ?? string.Empty,
                PermissionIds: request.PermissionIds ?? [],
                ActorId:       _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(
            nameof(GetRole),
            new { id = result.Value },
            new { id = result.Value });
    }

    /// <summary>
    /// Updates a custom role's name, description, and permission set.
    /// System roles are immutable and will return 400.
    /// PUT /api/identity/roles/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = DefaultPermissions.RoleManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await _mediator.Send(
            new UpdateRoleCommand(
                RoleId:        id,
                Name:          request.Name,
                Description:   request.Description ?? string.Empty,
                PermissionIds: request.PermissionIds ?? [],
                ActorId:       _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Soft-deletes a custom role. System roles cannot be deleted.
    /// DELETE /api/identity/roles/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = DefaultPermissions.RoleManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteRoleCommand(RoleId: id, ActorId: _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
