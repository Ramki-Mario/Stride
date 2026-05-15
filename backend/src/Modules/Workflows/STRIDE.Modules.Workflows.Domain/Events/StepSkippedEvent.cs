using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record StepSkippedEvent(
    Guid StepInstanceId,
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid SkippedBy) : IDomainEvent;
