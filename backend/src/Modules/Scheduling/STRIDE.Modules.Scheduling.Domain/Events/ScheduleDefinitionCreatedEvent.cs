using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Scheduling.Domain.Events;

public sealed record ScheduleDefinitionCreatedEvent(
    Guid ScheduleId,
    Guid TenantId,
    string Name,
    Guid WorkflowDefinitionId,
    Guid CreatedBy) : IDomainEvent;
