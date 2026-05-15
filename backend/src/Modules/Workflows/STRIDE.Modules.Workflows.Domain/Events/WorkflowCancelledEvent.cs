using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowCancelledEvent(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid CancelledBy) : IDomainEvent;
