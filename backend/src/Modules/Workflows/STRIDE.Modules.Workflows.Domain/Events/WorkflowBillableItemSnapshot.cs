namespace STRIDE.Modules.Workflows.Domain.Events;

/// <summary>
/// Immutable snapshot of a single billable item carried inside
/// <see cref="WorkflowCompletedEvent"/> so downstream modules (e.g. Invoicing)
/// can create an invoice draft without querying back into the Workflows module.
/// </summary>
public sealed record WorkflowBillableItemSnapshot(
    string  Description,
    decimal Quantity,
    decimal UnitPrice,
    string  Unit);       // string representation of BillableUnit enum value
