using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Teams.Domain.Events;

public sealed record TeamCreatedEvent(
    Guid TeamId,
    Guid TenantId,
    string Name,
    Guid CreatedBy) : IDomainEvent;
