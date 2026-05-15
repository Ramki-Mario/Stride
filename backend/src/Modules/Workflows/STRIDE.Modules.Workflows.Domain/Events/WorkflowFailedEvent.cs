using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowFailedEvent(
    Guid WorkflowInstanceId,
    Guid TenantId,
    string Reason,
    Guid FailedBy) : IDomainEvent;
