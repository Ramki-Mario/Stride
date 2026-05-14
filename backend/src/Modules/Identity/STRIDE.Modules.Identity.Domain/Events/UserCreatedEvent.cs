using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Identity.Domain.Events;

public sealed record UserCreatedEvent(Guid UserId, Guid TenantId, string Email) : IDomainEvent;
