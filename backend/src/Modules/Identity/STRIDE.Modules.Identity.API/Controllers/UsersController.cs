using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.API.Dtos;
using STRIDE.Modules.Identity.Application.Commands.AssignRole;
using STRIDE.Modules.Identity.Application.Queries.GetUser;

namespace STRIDE.Modules.Identity.API.Controllers;

/// <summary>
/// Authenticated user-management endpoints.
/// All routes require a valid JWT Bearer token (enforced by [Authorize]).
/// </summary>
[ApiController]
[Authorize]
[Route("api/identity/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator    _mediator;
    private readonly ICurrentUser _currentUser;

    public UsersController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator    = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns the profile of a user by ID within the current tenant.
    /// GET /api/identity/users/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserQuery(id), ct);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Assigns a role to a user within the current tenant.
    /// POST /api/identity/users/{userId}/roles
    /// </summary>
    [HttpPost("{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new AssignRoleCommand(
                UserId:     userId,
                RoleId:     request.RoleId,
                AssignedBy: _currentUser.UserId),
            ct);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
