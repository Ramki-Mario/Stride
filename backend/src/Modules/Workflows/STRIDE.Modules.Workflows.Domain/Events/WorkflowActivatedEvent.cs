using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowActivatedEvent(
    Guid WorkflowDefinitionId,
    Guid TenantId,
    Guid ActivatedBy) : IDomainEvent;
