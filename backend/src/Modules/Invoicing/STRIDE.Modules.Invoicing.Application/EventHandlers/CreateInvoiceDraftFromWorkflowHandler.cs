using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Repositories;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Invoicing.Application.EventHandlers;

/// <summary>
/// Automatically creates a Draft invoice when a workflow instance completes.
///
/// Rules:
///   - Invoice number: "WF-{workflowInstanceId[..8].ToUpperInvariant()}"
///   - One line item per billable item; decimal qty is preserved by mapping each
///     item as (quantity=1, unitPrice=lineTotal) so the invoice schema (int quantity)
///     is not changed, and the original detail is embedded in the description.
///   - If the workflow has no billable items, a blank draft is still created.
///   - Idempotent: if a draft already exists for this workflow instance, nothing happens.
///   - Due date defaults to 30 days from today (editable before sending).
///   - Failures are caught and logged — they must NOT roll back the workflow completion.
/// </summary>
internal sealed class CreateInvoiceDraftFromWorkflowHandler
    : INotificationHandler<DomainEventNotification<WorkflowCompletedEvent>>
{
    private readonly IInvoiceRepository _repo;
    private readonly ILogger<CreateInvoiceDraftFromWorkflowHandler> _logger;

    public CreateInvoiceDraftFromWorkflowHandler(
        IInvoiceRepository repo,
        ILogger<CreateInvoiceDraftFromWorkflowHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<WorkflowCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        // ── Idempotency check ─────────────────────────────────────────────────
        var existing = await _repo.GetBySourceWorkflowInstanceIdAsync(
            e.TenantId, e.WorkflowInstanceId, cancellationToken);

        if (existing is not null)
        {
            _logger.LogDebug(
                "Draft invoice already exists for workflow instance {Id} — skipping auto-creation.",
                e.WorkflowInstanceId);
            return;
        }

        // ── Build invoice ─────────────────────────────────────────────────────
        var invoiceNumber = $"WF-{e.WorkflowInstanceId.ToString("N")[..8].ToUpperInvariant()}";
        var clientName    = string.IsNullOrWhiteSpace(e.WorkflowName) ? "Auto-generated" : e.WorkflowName;
        var dueDate       = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        var invoice = Invoice.Generate(new NewInvoice(
            TenantId:                 e.TenantId,
            InvoiceNumber:            invoiceNumber,
            ClientName:               clientName,
            ClientEmail:              null,          // populated by staff before sending
            Currency:                 "GBP",
            DueDate:                  dueDate,
            CreatedBy:                e.StartedBy,
            Notes:                    $"Auto-generated draft from workflow '{e.WorkflowName}' (instance {e.WorkflowInstanceId}).",
            ClientId:                 e.ClientId,
            SourceWorkflowInstanceId: e.WorkflowInstanceId));

        // ── Map billable items → invoice line items ───────────────────────────
        // BillableItem.Quantity is decimal; InvoiceLineItem.Quantity is int.
        // Map each item as (quantity=1, unitPrice=lineTotal) to preserve the exact
        // monetary amount. The original qty/unit detail is embedded in the description.
        var items = e.BillableItems ?? [];
        foreach (var item in items)
        {
            var lineTotal   = Math.Round(item.Quantity * item.UnitPrice, 2);
            var description = $"{item.Description} ({item.Quantity} × {item.Unit} @ £{item.UnitPrice:F2})";
            invoice.AddLineItem(description, lineTotal, quantity: 1);
        }

        // ── Persist ───────────────────────────────────────────────────────────
        try
        {
            await _repo.AddAsync(invoice, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Auto-created draft invoice {Number} (id={Id}) for workflow instance {InstanceId}.",
                invoiceNumber, invoice.Id, e.WorkflowInstanceId);
        }
        catch (Exception ex)
        {
            // Swallow — auto-draft failure must never roll back the completed workflow.
            _logger.LogError(ex,
                "Failed to auto-create draft invoice for workflow instance {Id}.", e.WorkflowInstanceId);
        }
    }
}
