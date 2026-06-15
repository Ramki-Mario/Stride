using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.KitOps.Domain.Events;

public sealed record KitReservationCancelledEvent(
    Guid ReservationId,
    Guid KitItemId,
    Guid TenantId,
    Guid CancelledByUserId) : IDomainEvent;
