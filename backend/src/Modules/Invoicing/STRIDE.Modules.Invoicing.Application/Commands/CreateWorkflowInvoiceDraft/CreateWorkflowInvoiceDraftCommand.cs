using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Invoicing.Application.Commands.CreateWorkflowInvoiceDraft;

/// <summary>
/// Manually triggers auto-draft invoice creation from a workflow instance.
/// Used by the "Create Invoice" button on the workflow instance detail page
/// as a fallback when the automatic event handler did not fire (e.g. event was
/// missed, or the workflow completed before US-154 was deployed).
///
/// Idempotent — if a draft already exists for this workflow instance the
/// existing invoice's ID is returned and no duplicate is created.
/// </summary>
public sealed record CreateWorkflowInvoiceDraftBillableItem(
    string  Description,
    decimal Quantity,
    decimal UnitPrice,
    string  Unit);

public sealed record CreateWorkflowInvoiceDraftCommand(
    Guid   TenantId,
    Guid   WorkflowInstanceId,
    string WorkflowName,
    Guid   CreatedBy,
    Guid?  ClientId = null,
    IReadOnlyList<CreateWorkflowInvoiceDraftBillableItem>? BillableItems = null)
    : IRequest<Result<Guid>>;
