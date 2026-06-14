using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.API.Authorization;
using STRIDE.Modules.Identity.API.Dtos;
using STRIDE.Modules.Identity.Application.Commands.AcceptInvite;
using STRIDE.Modules.Identity.Application.Commands.AssignRole;
using STRIDE.Modules.Identity.Application.Queries.GetUser;
using STRIDE.Modules.Identity.Application.Queries.ValidateInviteToken;

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
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Validates an invite token without consuming it — returns the invitee's email+name.
    /// GET /api/identity/users/accept-invite/validate?token=xxx
    /// </summary>
    [HttpGet("accept-invite/validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InviteTokenInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateInviteToken(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ValidateInviteTokenQuery(token), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Validates the invite token, sets the user's password, activates their account,
    /// and returns a full token pair (auto-login).
    /// POST /api/identity/users/accept-invite
    /// </summary>
    [HttpPost("accept-invite")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await _mediator.Send(
            new AcceptInviteCommand(request.Token, request.Password, request.ConfirmPassword),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Assigns a role to a user within the current tenant.
    /// Requires the <c>Admin</c> role — only admins may grant roles.
    /// POST /api/identity/users/{userId}/roles
    /// </summary>
    [HttpPost("{userId:guid}/roles")]
    [Authorize(Policy = Policies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignRoleCommand(
                UserId:     userId,
                RoleId:     request.RoleId,
                AssignedBy: _currentUser.UserId),
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
