using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowPausedEvent(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid PausedBy) : IDomainEvent;
