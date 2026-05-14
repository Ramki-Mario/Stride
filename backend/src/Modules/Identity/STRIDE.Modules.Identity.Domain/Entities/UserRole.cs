using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class UserRole : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    private UserRole() { }

    public static UserRole Create(Guid tenantId, Guid userId, Guid roleId, Guid createdBy)
    {
        return new UserRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Revoke()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
