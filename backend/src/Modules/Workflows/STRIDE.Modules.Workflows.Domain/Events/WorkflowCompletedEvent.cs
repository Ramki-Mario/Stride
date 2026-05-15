using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowCompletedEvent(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid StartedBy) : IDomainEvent;
