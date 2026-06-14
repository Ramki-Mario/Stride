using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

/// <summary>
/// System-wide permission key. Not tenant-scoped — shared across all tenants.
/// Seeded once via data migration; roles reference these by FK.
/// </summary>
public sealed class Permission : BaseEntity<Guid>
{
    public string Key         { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Permission() { }

    public static Permission Create(string key, string description)
    {
        return new Permission
        {
            Id          = Guid.NewGuid(),
            Key         = key.Trim().ToLowerInvariant(),
            Description = description.Trim()
        };
    }
}
