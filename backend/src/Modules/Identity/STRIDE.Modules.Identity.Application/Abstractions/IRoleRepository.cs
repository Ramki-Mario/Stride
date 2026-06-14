using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface IRoleRepository
{
    /// <summary>Returns a role by primary key within the current tenant scope.</summary>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Looks up a role by its normalised name (upper-case) within the current tenant.</summary>
    Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    /// <summary>Returns all active (non-deleted) roles in the current tenant.</summary>
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns true if a non-deleted role with this normalised name already exists in the tenant.</summary>
    Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    /// <summary>Stages a new role for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>Marks an existing role as modified. Caller must call SaveChangesAsync.</summary>
    void Update(Role role);

    /// <summary>Returns the IDs of active users currently assigned this role within the current tenant.</summary>
    Task<IReadOnlyList<Guid>> GetAssignedUserIdsAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
