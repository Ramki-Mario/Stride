using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Clients.API.DTOs;
using STRIDE.Modules.Clients.Application.Commands.CreateClient;
using STRIDE.Modules.Clients.Application.Commands.DeactivateClient;
using STRIDE.Modules.Clients.Application.Commands.ReactivateClient;
using STRIDE.Modules.Clients.Application.Commands.UpdateClient;
using STRIDE.Modules.Clients.Application.Queries.GetClientById;
using STRIDE.Modules.Clients.Application.Queries.GetClients;

namespace STRIDE.Modules.Clients.API.Controllers;

/// <summary>
/// Tenant client management endpoints.
///
///   GET    /api/clients              — paged list
///   POST   /api/clients              — create new client
///   GET    /api/clients/{id}         — get detail
///   PUT    /api/clients/{id}         — update client
///   PUT    /api/clients/{id}/deactivate — deactivate client
/// </summary>
[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public ClientsController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClients(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] int?    status   = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetClientsQuery(_tenantContext.TenantId, search, status, page, pageSize), cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateClient(
        [FromBody] CreateClientRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateClientCommand(
                _tenantContext.TenantId,
                request.Name,
                request.ContactPerson,
                request.Email,
                request.Phone,
                request.Address,
                request.Notes,
                _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetClient), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClient(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetClientByIdQuery(_tenantContext.TenantId, id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClient(
        Guid id, [FromBody] UpdateClientRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateClientCommand(
                _tenantContext.TenantId,
                id,
                request.Name,
                request.ContactPerson,
                request.Email,
                request.Phone,
                request.Address,
                request.Notes,
                _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error == "Client not found."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateClient(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeactivateClientCommand(_tenantContext.TenantId, id, _currentUser.UserId), cancellationToken);

        if (result.IsFailure)
            return result.Error == "Client not found."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateClient(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReactivateClientCommand(_tenantContext.TenantId, id, _currentUser.UserId), cancellationToken);

        if (result.IsFailure)
            return result.Error == "Client not found."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }
}
