using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record StepFailedEvent(
    Guid   StepInstanceId,
    Guid   WorkflowInstanceId,
    Guid   TenantId,
    string Reason,
    Guid   FailedBy,
    string StepName = "") : IDomainEvent;
