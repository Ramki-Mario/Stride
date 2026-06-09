using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowHaltedEvent(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid RejectedStepId,
    Guid HaltedBy) : IDomainEvent;
