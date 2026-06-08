using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Teams.API.Models;
using STRIDE.Modules.Teams.Application.Commands.CreateTeam;
using STRIDE.Modules.Teams.Application.Commands.DeactivateTeam;
using STRIDE.Modules.Teams.Application.Commands.ReactivateTeam;
using STRIDE.Modules.Teams.Application.Commands.UpdateTeam;
using STRIDE.Modules.Teams.Application.Queries.GetTeamById;
using STRIDE.Modules.Teams.Application.Queries.GetTeams;

namespace STRIDE.Modules.Teams.API.Controllers;

/// <summary>
/// Tenant team / department management endpoints.
///
///   GET    /api/teams              — paged list
///   POST   /api/teams              — create new team
///   GET    /api/teams/{id}         — get team detail
///   PUT    /api/teams/{id}         — update team
///   PUT    /api/teams/{id}/deactivate — deactivate team
///   PUT    /api/teams/{id}/reactivate — reactivate team
/// </summary>
[ApiController]
[Authorize]
[Route("api/teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public TeamsController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeams(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] int?    status   = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTeamsQuery(_tenantContext.TenantId, search, status, page, pageSize),
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTeam(
        [FromBody] CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateTeamCommand(
                _tenantContext.TenantId,
                request.Name,
                request.Description,
                request.ParentTeamId,
                _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetTeam), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeam(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetTeamByIdQuery(_tenantContext.TenantId, id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeam(
        Guid id, [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateTeamCommand(
                _tenantContext.TenantId,
                id,
                request.Name,
                request.Description,
                request.ParentTeamId,
                _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTeam(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeactivateTeamCommand(_tenantContext.TenantId, id, _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateTeam(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReactivateTeamCommand(_tenantContext.TenantId, id, _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }
}
