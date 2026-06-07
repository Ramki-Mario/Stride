namespace STRIDE.Modules.Invoicing.API.DTOs;

/// <summary>
/// Request body for POST /api/invoicing/invoices/from-workflow/{workflowInstanceId}.
/// The caller (Angular via BFF) supplies the workflow context that the Invoicing module
/// cannot reach directly (cross-module boundary).
/// </summary>
public sealed record CreateWorkflowInvoiceBillableItemRequest(
    string  Description,
    decimal Quantity,
    decimal UnitPrice,
    string  Unit);

public sealed record CreateWorkflowInvoiceRequest(
    string  WorkflowName,
    Guid?   ClientId,
    IReadOnlyList<CreateWorkflowInvoiceBillableItemRequest>? BillableItems = null);
