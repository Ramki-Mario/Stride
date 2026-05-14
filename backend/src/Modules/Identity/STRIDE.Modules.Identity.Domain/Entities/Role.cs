using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class Role : AuditableEntity
{
    private readonly List<RolePermission> _permissions = new();

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public IReadOnlyList<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role() { }

    public static Role Create(Guid tenantId, string name, string description, Guid createdBy)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            NormalizedName = name.Trim().ToUpperInvariant(),
            Description = description.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void GrantPermission(Permission permission, Guid grantedBy)
    {
        if (_permissions.Any(p => p.PermissionId == permission.Id && !p.IsDeleted))
            return;

        _permissions.Add(RolePermission.Create(TenantId, Id, permission.Id, grantedBy));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokePermission(Guid permissionId)
    {
        var rp = _permissions.FirstOrDefault(p => p.PermissionId == permissionId && !p.IsDeleted);
        rp?.Revoke();
        UpdatedAt = DateTime.UtcNow;
    }
}
