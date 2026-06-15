using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.API.Models;
using STRIDE.Modules.KitOps.Application.Commands.CreateKitItem;
using STRIDE.Modules.KitOps.Application.Commands.DeactivateKitItem;
using STRIDE.Modules.KitOps.Application.Commands.ReactivateKitItem;
using STRIDE.Modules.KitOps.Application.Commands.UpdateKitItem;
using STRIDE.Modules.KitOps.Application.Queries.GetKitCatalog;
using STRIDE.Modules.KitOps.Application.Queries.GetKitItemAvailability;
using STRIDE.Modules.KitOps.Application.Queries.GetKitItemById;
using STRIDE.Modules.KitOps.Application.Queries.GetKitItems;

namespace STRIDE.Modules.KitOps.API.Controllers;

/// <summary>
/// Kit catalog management endpoints.
///
///   GET    /api/kit-items              — paged list with optional search/category/isActive filters
///   POST   /api/kit-items              — create new kit item (Admin only)
///   GET    /api/kit-items/{id}         — get kit item detail
///   GET    /api/kit-items/{id}/availability — live availability snapshot
///   PUT    /api/kit-items/{id}         — update kit item (Admin only)
///   PUT    /api/kit-items/{id}/deactivate — deactivate (Admin only)
///   PUT    /api/kit-items/{id}/reactivate — reactivate (Admin only)
/// </summary>
[ApiController]
[Authorize]
[Route("api/kit-items")]
public sealed class KitItemsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public KitItemsController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKitItems(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] string? category = null,
        [FromQuery] bool?   isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetKitItemsQuery(_tenantContext.TenantId, search, category, isActive, page, pageSize),
            cancellationToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns all active kit items with live availability counts — used by the field user browse screen.
    /// </summary>
    [HttpGet("catalog")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetKitCatalogQuery(_tenantContext.TenantId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateKitItem(
        [FromBody] CreateKitItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateKitItemCommand(
                _tenantContext.TenantId,
                request.Name,
                request.Category,
                request.Description,
                request.TotalQuantity,
                _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetKitItem), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKitItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetKitItemByIdQuery(_tenantContext.TenantId, id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/availability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailability(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetKitItemAvailabilityQuery(_tenantContext.TenantId, id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateKitItem(
        Guid id, [FromBody] UpdateKitItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateKitItemCommand(
                _tenantContext.TenantId,
                id,
                request.Name,
                request.Category,
                request.Description,
                request.TotalQuantity,
                _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateKitItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeactivateKitItemCommand(_tenantContext.TenantId, id, _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/reactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateKitItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReactivateKitItemCommand(_tenantContext.TenantId, id, _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }
}
