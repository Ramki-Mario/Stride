using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Administration.API.DTOs;
using STRIDE.Modules.Administration.Application.Commands.DeactivateUser;
using STRIDE.Modules.Administration.Application.Commands.InviteUser;
using STRIDE.Modules.Administration.Application.Commands.ReactivateUser;
using STRIDE.Modules.Administration.Application.Commands.UpdateUserRole;
using STRIDE.Modules.Administration.Application.Queries.GetUsers;

namespace STRIDE.Modules.Administration.API.Controllers;

/// <summary>
/// Tenant user management endpoints for the Administration module.
///
///   GET    /api/administration/users              — paged, filtered user list
///   POST   /api/administration/users/invite       — invite new user (creates pending account)
///   PUT    /api/administration/users/{id}/role    — change user role
///   PUT    /api/administration/users/{id}/deactivate   — deactivate user
///   PUT    /api/administration/users/{id}/reactivate   — reactivate user
/// </summary>
[ApiController]
[Authorize]
[Route("api/administration/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public UsersController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns a paged, searchable list of users in the current tenant.
    /// GET /api/administration/users?page=1&amp;pageSize=20&amp;search=&amp;role=&amp;status=
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int    page     = 1,
        [FromQuery] int    pageSize = 20,
        [FromQuery] string? search  = null,
        [FromQuery] string? role    = null,
        [FromQuery] string? status  = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetUsersQuery(_tenantContext.TenantId, search, role, status, page, pageSize), cancellationToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// Invites a new user to the tenant (creates a pending account).
    /// POST /api/administration/users/invite
    /// </summary>
    [HttpPost("invite")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InviteUser(
        [FromBody] InviteUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new InviteUserCommand(
                _tenantContext.TenantId,
                request.Email,
                request.DisplayName,
                request.Role,
                _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetUsers), new { }, new { id = result.Value });
    }

    /// <summary>
    /// Changes the role of a user within the current tenant.
    /// PUT /api/administration/users/{id}/role
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserRole(
        Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateUserRoleCommand(_tenantContext.TenantId, id, request.Role, _currentUser.UserId), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Deactivates a user account (sets IsActive = false).
    /// PUT /api/administration/users/{id}/deactivate
    /// </summary>
    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeactivateUserCommand(_tenantContext.TenantId, id, _currentUser.UserId), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Reactivates a user account (sets IsActive = true, IsPending = false).
    /// PUT /api/administration/users/{id}/reactivate
    /// </summary>
    [HttpPut("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReactivateUserCommand(_tenantContext.TenantId, id), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }
}
