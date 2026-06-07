using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Clients.Domain.Events;

public sealed record ClientDeactivatedEvent(
    Guid ClientId,
    Guid TenantId,
    Guid DeactivatedBy) : IDomainEvent;
