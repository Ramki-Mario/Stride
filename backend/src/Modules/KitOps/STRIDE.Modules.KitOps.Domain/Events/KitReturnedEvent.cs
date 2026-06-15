using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.KitOps.Domain.Events;

public sealed record KitReturnedEvent(
    Guid     CheckoutId,
    Guid     KitItemId,
    Guid     TenantId,
    Guid     ReturnedByUserId,
    DateTime ReturnedAt) : IDomainEvent;
