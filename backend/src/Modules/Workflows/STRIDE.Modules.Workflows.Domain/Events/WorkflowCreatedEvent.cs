using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowCreatedEvent(
    Guid WorkflowDefinitionId,
    Guid TenantId,
    string Name,
    Guid CreatedBy) : IDomainEvent;
