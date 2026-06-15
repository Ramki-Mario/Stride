using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.KitOps.Domain.Events;

public sealed record KitReservationCreatedEvent(
    Guid ReservationId,
    Guid KitItemId,
    Guid TenantId,
    Guid RequestedByUserId) : IDomainEvent;
