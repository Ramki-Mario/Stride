using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.KitOps.Domain.Events;

public sealed record KitItemCreatedEvent(
    Guid   Id,
    Guid   TenantId,
    string Name,
    Guid   CreatedBy) : IDomainEvent;
