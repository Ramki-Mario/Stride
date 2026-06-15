using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.API.Models;
using STRIDE.Modules.KitOps.Application.Commands.CheckoutKitItem;
using STRIDE.Modules.KitOps.Application.Commands.ReturnKitCheckout;
using STRIDE.Modules.KitOps.Application.Queries.GetMyKitCheckouts;

namespace STRIDE.Modules.KitOps.API.Controllers;

/// <summary>
/// Kit checkout / return lifecycle endpoints for field users.
///
///   POST   /api/kit-checkouts            — check out an available unit for N days
///   PUT    /api/kit-checkouts/{id}/return — mark a checkout returned
///
/// Any authenticated user may check out and return kit (field operation);
/// catalog management lives on <c>/api/kit-items</c> and is Admin-only.
/// </summary>
[ApiController]
[Authorize]
[Route("api/kit-checkouts")]
public sealed class KitCheckoutsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public KitCheckoutsController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutKitItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CheckoutKitItemCommand(
                _tenantContext.TenantId,
                request.KitItemId,
                _currentUser.UserId,
                request.Days,
                request.Notes),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return Created($"/api/kit-checkouts/{result.Value}", new { id = result.Value });
    }

    [HttpGet("my")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCheckouts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMyKitCheckoutsQuery(_tenantContext.TenantId, _currentUser.UserId),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Return(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReturnKitCheckoutCommand(_tenantContext.TenantId, id, _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }
}
