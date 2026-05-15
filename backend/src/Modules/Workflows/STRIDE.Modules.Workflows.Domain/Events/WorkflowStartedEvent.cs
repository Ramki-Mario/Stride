using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record WorkflowStartedEvent(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid DefinitionId,
    string WorkflowName,
    Guid StartedBy) : IDomainEvent;
