using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Invoicing.API.DTOs;
using STRIDE.Modules.Invoicing.Application.Commands.GenerateInvoice;
using STRIDE.Modules.Invoicing.Application.Commands.MarkInvoicePaid;
using STRIDE.Modules.Invoicing.Application.Commands.SendInvoice;
using STRIDE.Modules.Invoicing.Application.Commands.VoidInvoice;
using STRIDE.Modules.Invoicing.Application.Queries.GetInvoiceById;
using STRIDE.Modules.Invoicing.Application.Queries.GetInvoices;

namespace STRIDE.Modules.Invoicing.API.Controllers;

/// <summary>
/// Tenant invoice management endpoints.
///
///   GET    /api/invoicing/invoices              — paged list
///   POST   /api/invoicing/invoices              — generate new invoice
///   GET    /api/invoicing/invoices/{id}         — get detail
///   PUT    /api/invoicing/invoices/{id}/send    — send to client
///   PUT    /api/invoicing/invoices/{id}/paid    — mark as paid
///   PUT    /api/invoicing/invoices/{id}/void    — void invoice
/// </summary>
[ApiController]
[Authorize]
[Route("api/invoicing/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public InvoicesController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] int?    status   = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetInvoicesQuery(_tenantContext.TenantId, search, status, page, pageSize), cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateInvoice(
        [FromBody] GenerateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var lineItems = request.LineItems
            .Select(l => new GenerateInvoiceLineItem(l.Description, l.UnitPrice, l.Quantity))
            .ToList();

        var result = await _mediator.Send(
            new GenerateInvoiceCommand(
                _tenantContext.TenantId,
                request.InvoiceNumber,
                request.ClientName,
                request.ClientEmail,
                request.Currency,
                request.DueDate,
                request.Notes,
                lineItems,
                _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetInvoice), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetInvoiceByIdQuery(_tenantContext.TenantId, id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/send")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendInvoice(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SendInvoiceCommand(_tenantContext.TenantId, id, _currentUser.UserId), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/paid")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new MarkInvoicePaidCommand(_tenantContext.TenantId, id, _currentUser.UserId), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/void")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VoidInvoice(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new VoidInvoiceCommand(_tenantContext.TenantId, id, _currentUser.UserId), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }
}
