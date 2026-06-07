using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Commands.CreateWorkflowInvoiceDraft;

/// <summary>
/// Handles manual (on-demand) invoice draft creation from a workflow instance.
/// Uses the same idempotency and mapping rules as
/// <see cref="EventHandlers.CreateInvoiceDraftFromWorkflowHandler"/>.
/// </summary>
internal sealed class CreateWorkflowInvoiceDraftCommandHandler
    : IRequestHandler<CreateWorkflowInvoiceDraftCommand, Result<Guid>>
{
    private readonly IInvoiceRepository _repo;
    private readonly ILogger<CreateWorkflowInvoiceDraftCommandHandler> _logger;

    public CreateWorkflowInvoiceDraftCommandHandler(
        IInvoiceRepository repo,
        ILogger<CreateWorkflowInvoiceDraftCommandHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        CreateWorkflowInvoiceDraftCommand request,
        CancellationToken cancellationToken)
    {
        // ── Idempotency check ─────────────────────────────────────────────────
        var existing = await _repo.GetBySourceWorkflowInstanceIdAsync(
            request.TenantId, request.WorkflowInstanceId, cancellationToken);

        if (existing is not null)
        {
            _logger.LogDebug(
                "Invoice draft already exists for workflow instance {Id} — returning existing id.",
                request.WorkflowInstanceId);
            return Result.Success(existing.Id);
        }

        // ── Build invoice ─────────────────────────────────────────────────────
        var invoiceNumber = $"WF-{request.WorkflowInstanceId.ToString("N")[..8].ToUpperInvariant()}";
        var clientName    = string.IsNullOrWhiteSpace(request.WorkflowName)
                               ? "Auto-generated"
                               : request.WorkflowName;
        var dueDate       = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        var invoice = Invoice.Generate(new NewInvoice(
            TenantId:                 request.TenantId,
            InvoiceNumber:            invoiceNumber,
            ClientName:               clientName,
            ClientEmail:              null,
            Currency:                 "GBP",
            DueDate:                  dueDate,
            CreatedBy:                request.CreatedBy,
            Notes:                    $"Manually created draft from workflow '{request.WorkflowName}' (instance {request.WorkflowInstanceId}).",
            ClientId:                 request.ClientId,
            SourceWorkflowInstanceId: request.WorkflowInstanceId));

        // ── Map billable items → line items ───────────────────────────────────
        foreach (var item in request.BillableItems ?? [])
        {
            var lineTotal   = Math.Round(item.Quantity * item.UnitPrice, 2);
            var description = $"{item.Description} ({item.Quantity} × {item.Unit} @ £{item.UnitPrice:F2})";
            invoice.AddLineItem(description, lineTotal, quantity: 1);
        }

        await _repo.AddAsync(invoice, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Manually created draft invoice {Number} (id={Id}) for workflow instance {InstanceId}.",
            invoiceNumber, invoice.Id, request.WorkflowInstanceId);

        return Result.Success(invoice.Id);
    }
}
