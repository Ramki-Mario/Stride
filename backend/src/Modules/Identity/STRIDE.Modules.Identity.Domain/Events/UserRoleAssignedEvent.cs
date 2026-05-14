using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Identity.Domain.Events;

public sealed record UserRoleAssignedEvent(Guid UserId, Guid TenantId, Guid RoleId) : IDomainEvent;
