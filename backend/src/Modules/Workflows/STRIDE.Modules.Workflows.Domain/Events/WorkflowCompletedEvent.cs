using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

/// <summary>
/// Raised when all steps in a workflow instance reach a terminal state and
/// the instance transitions to <c>Completed</c>.
/// Carries a billing snapshot so the Invoicing module can create a draft invoice
/// without querying back into the Workflows module.
/// </summary>
public sealed record WorkflowCompletedEvent(
    Guid   WorkflowInstanceId,
    Guid   TenantId,
    Guid   StartedBy,
    string WorkflowName = "",
    Guid?  ClientId     = null,
    IReadOnlyList<WorkflowBillableItemSnapshot>? BillableItems = null) : IDomainEvent;
