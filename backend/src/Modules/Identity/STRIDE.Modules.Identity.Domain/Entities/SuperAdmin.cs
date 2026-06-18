using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

/// <summary>
/// Platform-level super-admin record. Not scoped to any tenant.
/// Presence in this table grants the "superadmin" JWT claim on login,
/// which unlocks the /api/platform/* endpoints and admin.strydesuite.com.
/// </summary>
public sealed class SuperAdmin : BaseEntity<Guid>
{
    public string NormalizedEmail { get; private set; } = string.Empty;
    public DateTime AddedAt { get; private set; }

    private SuperAdmin() { }

    public static SuperAdmin Create(string email) => new()
    {
        Id              = Guid.NewGuid(),
        NormalizedEmail = email.Trim().ToUpperInvariant(),
        AddedAt         = DateTime.UtcNow
    };
}
