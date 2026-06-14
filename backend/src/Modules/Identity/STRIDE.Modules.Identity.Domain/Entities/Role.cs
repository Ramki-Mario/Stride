using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class Role : AuditableEntity
{
    private readonly List<RolePermission> _permissions = new();

    public string Name           { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string Description    { get; private set; } = string.Empty;
    /// <summary>System roles cannot be deleted or have their permissions removed.</summary>
    public bool   IsSystemRole   { get; private set; }

    public IReadOnlyList<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role() { }

    public static Role Create(Guid tenantId, string name, string description, Guid createdBy)
    {
        return new Role
        {
            Id             = Guid.NewGuid(),
            TenantId       = tenantId,
            Name           = name.Trim(),
            NormalizedName = name.Trim().ToUpperInvariant(),
            Description    = description.Trim(),
            IsSystemRole   = false,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
            CreatedBy      = createdBy
        };
    }

    public static Role CreateSystemRole(Guid tenantId, string name, string description, Guid createdBy)
    {
        return new Role
        {
            Id             = Guid.NewGuid(),
            TenantId       = tenantId,
            Name           = name.Trim(),
            NormalizedName = name.Trim().ToUpperInvariant(),
            Description    = description.Trim(),
            IsSystemRole   = true,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
            CreatedBy      = createdBy
        };
    }

    public void Update(string name, string description)
    {
        Name           = name.Trim();
        NormalizedName = name.Trim().ToUpperInvariant();
        Description    = description.Trim();
        UpdatedAt      = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void GrantPermission(Permission permission, Guid grantedBy)
    {
        if (_permissions.Any(p => p.PermissionId == permission.Id && !p.IsDeleted))
            return;

        // If a soft-deleted entry already exists, restore it to avoid unique-constraint violation.
        var revoked = _permissions.FirstOrDefault(p => p.PermissionId == permission.Id && p.IsDeleted);
        if (revoked is not null)
        {
            revoked.Restore();
            UpdatedAt = DateTime.UtcNow;
            return;
        }

        _permissions.Add(RolePermission.Create(TenantId, Id, permission.Id, grantedBy));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokePermission(Guid permissionId)
    {
        var rp = _permissions.FirstOrDefault(p => p.PermissionId == permissionId && !p.IsDeleted);
        rp?.Revoke();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SyncPermissions(
        IReadOnlyList<Permission> desired,
        Guid actorId)
    {
        // Revoke any active permissions not in the desired set.
        foreach (var rp in _permissions.Where(p => !p.IsDeleted))
        {
            if (!desired.Any(d => d.Id == rp.PermissionId))
                rp.Revoke();
        }

        // Grant (or restore) each desired permission.
        foreach (var permission in desired)
            GrantPermission(permission, actorId);
    }
}
