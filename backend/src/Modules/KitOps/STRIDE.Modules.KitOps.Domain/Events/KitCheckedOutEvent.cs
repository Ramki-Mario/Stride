using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.KitOps.Domain.Events;

public sealed record KitCheckedOutEvent(
    Guid     CheckoutId,
    Guid     KitItemId,
    Guid     TenantId,
    Guid     CheckedOutByUserId,
    DateTime ExpectedReturnAt) : IDomainEvent;
