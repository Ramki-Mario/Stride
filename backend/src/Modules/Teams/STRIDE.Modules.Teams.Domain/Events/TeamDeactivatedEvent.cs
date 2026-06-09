using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Teams.Domain.Events;

public sealed record TeamDeactivatedEvent(
    Guid TeamId,
    Guid TenantId,
    Guid DeactivatedBy) : IDomainEvent;
