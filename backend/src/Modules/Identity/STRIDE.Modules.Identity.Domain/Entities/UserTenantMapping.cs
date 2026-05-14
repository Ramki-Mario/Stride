using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class UserTenantMapping : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string RoleName { get; private set; } = string.Empty;

    private UserTenantMapping() { }

    public static UserTenantMapping Create(Guid tenantId, Guid userId, string roleName, Guid createdBy)
    {
        return new UserTenantMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            RoleName = roleName.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void UpdateRole(string roleName)
    {
        RoleName = roleName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Remove()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
