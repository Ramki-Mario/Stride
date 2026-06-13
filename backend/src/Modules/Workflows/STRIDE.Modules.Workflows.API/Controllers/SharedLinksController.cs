using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.API.Dtos;
using STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;
using STRIDE.Modules.Workflows.Application.Commands.RevokeSharedLink;
using STRIDE.Modules.Workflows.Application.Queries.ListSharedLinks;

namespace STRIDE.Modules.Workflows.API.Controllers;

/// <summary>
/// Shareable secure links for a workflow instance (EP-058 / US-177).
///
///   GET    /api/workflows/instances/{instanceId}/share              — list active links
///   POST   /api/workflows/instances/{instanceId}/share              — create a link
///   DELETE /api/workflows/instances/{instanceId}/share/{linkId}     — revoke a link
/// </summary>
[ApiController]
[Authorize]
[Route("api/workflows/instances/{instanceId:guid}/share")]
public sealed class SharedLinksController : ControllerBase
{
    private const string NotFoundFragment = "not found";

    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public SharedLinksController(
        IMediator mediator,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>Lists the active (non-revoked) links for the instance, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SharedLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLinks(Guid instanceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListSharedLinksQuery(instanceId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>Creates a new shareable link and returns it (including the full public URL).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SharedLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateLink(
        Guid instanceId,
        [FromBody] CreateSharedLinkRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateSharedLinkCommand(
                TenantId:           _tenantContext.TenantId,
                WorkflowInstanceId: instanceId,
                CreatedByUserId:    _currentUser.UserId,
                ExpiryDays:         request?.ExpiryDays),
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { detail = result.Error });
            return BadRequest(new { detail = result.Error });
        }

        return CreatedAtAction(nameof(ListLinks), new { instanceId }, result.Value);
    }

    /// <summary>Revokes a link, immediately invalidating it.</summary>
    [HttpDelete("{linkId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeLink(
        Guid instanceId,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RevokeSharedLinkCommand(instanceId, linkId), cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new { detail = result.Error });

        return NoContent();
    }
}
