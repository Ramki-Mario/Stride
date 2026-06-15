using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.KitOps.Domain.Events;

public sealed record KitItemDeactivatedEvent(
    Guid Id,
    Guid TenantId,
    Guid DeactivatedBy) : IDomainEvent;
