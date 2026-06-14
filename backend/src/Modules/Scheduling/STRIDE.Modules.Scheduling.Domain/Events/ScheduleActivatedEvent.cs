using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Scheduling.Domain.Events;

public sealed record ScheduleActivatedEvent(
    Guid ScheduleId,
    Guid TenantId,
    Guid ActivatedBy) : IDomainEvent;
