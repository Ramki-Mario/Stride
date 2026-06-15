using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.KitOps.Domain.Events;

public sealed record KitItemReactivatedEvent(
    Guid Id,
    Guid TenantId,
    Guid ReactivatedBy) : IDomainEvent;
