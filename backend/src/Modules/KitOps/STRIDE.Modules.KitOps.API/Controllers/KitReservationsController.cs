using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.API.Models;
using STRIDE.Modules.KitOps.Application.Commands.CancelKitReservation;
using STRIDE.Modules.KitOps.Application.Commands.CreateKitReservation;
using STRIDE.Modules.KitOps.Application.Queries.GetMyKitReservations;

namespace STRIDE.Modules.KitOps.API.Controllers;

/// <summary>
/// Kit reservation / booking-request endpoints for field users.
///
///   POST   /api/kit-reservations            — request a kit that is currently out
///   PUT    /api/kit-reservations/{id}/cancel — cancel your own pending request
///
/// Reservations queue in FIFO order; the oldest pending request is fulfilled
/// automatically when a unit of the kit is returned.
/// </summary>
[ApiController]
[Authorize]
[Route("api/kit-reservations")]
[RequiresModule(ModuleNames.KitOps)]
public sealed class KitReservationsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public KitReservationsController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateKitReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateKitReservationCommand(
                _tenantContext.TenantId,
                request.KitItemId,
                _currentUser.UserId,
                request.Notes),
            cancellationToken);

        if (result.IsFailure)
            return result.Error!.Contains("not found")
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return Created($"/api/kit-reservations/{result.Value}", new { id = result.Value });
    }

    [HttpGet("my")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMyKitReservationsQuery(_tenantContext.TenantId, _currentUser.UserId),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelKitReservationCommand(_tenantContext.TenantId, id, _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found"))
                return NotFound(new { error = result.Error });
            if (result.Error == CancelKitReservationCommand.NotOwnerError)
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
