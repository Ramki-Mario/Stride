using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface IRoleRepository
{
    /// <summary>Returns a role by primary key within the current tenant scope.</summary>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Looks up a role by its normalised name (upper-case) within the current tenant.</summary>
    Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken ct = default);

    /// <summary>Returns all active (non-deleted) roles in the current tenant.</summary>
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns true if a non-deleted role with this normalised name already exists in the tenant.</summary>
    Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken ct = default);

    /// <summary>Stages a new role for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(Role role, CancellationToken ct = default);

    /// <summary>Marks an existing role as modified. Caller must call SaveChangesAsync.</summary>
    void Update(Role role);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
