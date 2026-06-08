using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

/// <summary>
/// Raised when a step's due date has passed and the step is not yet complete.
/// Consumed by the Notifications module to alert the assignee (or workflow starter).
/// </summary>
public sealed record StepOverdueEvent(
    Guid   StepInstanceId,
    Guid   WorkflowInstanceId,
    Guid   TenantId,
    Guid?  AssigneeId,
    Guid   StartedBy,
    string StepName = "") : IDomainEvent;
