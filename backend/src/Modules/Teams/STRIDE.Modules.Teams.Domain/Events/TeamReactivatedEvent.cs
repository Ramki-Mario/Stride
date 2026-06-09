using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Teams.Domain.Events;

public sealed record TeamReactivatedEvent(
    Guid TeamId,
    Guid TenantId,
    Guid ReactivatedBy) : IDomainEvent;
