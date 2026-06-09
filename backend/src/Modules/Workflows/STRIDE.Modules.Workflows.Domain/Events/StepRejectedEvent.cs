using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record StepRejectedEvent(
    Guid   StepInstanceId,
    Guid   WorkflowInstanceId,
    Guid   TenantId,
    Guid   RejectedBy,
    string StepName = "") : IDomainEvent;
