using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class Permission : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;

    private Permission() { }

    public static Permission Create(Guid tenantId, string resource, string action, Guid createdBy)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Resource = resource.Trim().ToLowerInvariant(),
            Action = action.Trim().ToLowerInvariant(),
            Name = $"{resource.Trim().ToLowerInvariant()}.{action.Trim().ToLowerInvariant()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}
