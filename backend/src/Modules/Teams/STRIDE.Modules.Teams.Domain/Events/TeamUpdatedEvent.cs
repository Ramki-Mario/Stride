using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Teams.Domain.Events;

public sealed record TeamUpdatedEvent(
    Guid TeamId,
    Guid TenantId,
    string Name,
    Guid UpdatedBy) : IDomainEvent;
