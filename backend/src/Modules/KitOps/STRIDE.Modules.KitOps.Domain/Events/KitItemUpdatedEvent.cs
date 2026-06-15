using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.KitOps.Domain.Events;

public sealed record KitItemUpdatedEvent(
    Guid   Id,
    Guid   TenantId,
    string Name,
    Guid   UpdatedBy) : IDomainEvent;
