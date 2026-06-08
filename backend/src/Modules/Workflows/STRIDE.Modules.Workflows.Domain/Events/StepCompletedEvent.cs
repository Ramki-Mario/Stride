using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record StepCompletedEvent(
    Guid   StepInstanceId,
    Guid   WorkflowInstanceId,
    Guid   TenantId,
    Guid   CompletedBy,
    string StepName = "") : IDomainEvent;
