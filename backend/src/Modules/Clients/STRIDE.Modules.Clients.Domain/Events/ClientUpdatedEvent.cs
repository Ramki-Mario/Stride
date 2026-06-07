using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Clients.Domain.Events;

public sealed record ClientUpdatedEvent(
    Guid ClientId,
    Guid TenantId,
    Guid UpdatedBy) : IDomainEvent;
