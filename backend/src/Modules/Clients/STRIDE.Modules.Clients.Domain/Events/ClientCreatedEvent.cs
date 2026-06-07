using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Clients.Domain.Events;

public sealed record ClientCreatedEvent(
    Guid ClientId,
    Guid TenantId,
    string Name,
    Guid CreatedBy) : IDomainEvent;
