using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

/// <summary>
/// Raised when a workflow instance's SLA deadline has passed and the workflow is still running.
/// Consumed by the Notifications module to alert the workflow initiator.
/// </summary>
public sealed record WorkflowSlaBreachedEvent(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid StartedBy,
    DateTime DeadlineAt) : IDomainEvent;
