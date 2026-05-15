using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowResumedEvent(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid ResumedBy) : IDomainEvent;
