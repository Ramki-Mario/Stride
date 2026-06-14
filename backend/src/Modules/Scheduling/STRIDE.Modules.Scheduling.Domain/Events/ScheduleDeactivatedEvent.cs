using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Scheduling.Domain.Events;

public sealed record ScheduleDeactivatedEvent(
    Guid ScheduleId,
    Guid TenantId,
    Guid DeactivatedBy) : IDomainEvent;
