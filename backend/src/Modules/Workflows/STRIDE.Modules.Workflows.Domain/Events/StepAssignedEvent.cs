using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record StepAssignedEvent(
    Guid StepInstanceId,
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid AssigneeId,
    Guid AssignedBy) : IDomainEvent;
