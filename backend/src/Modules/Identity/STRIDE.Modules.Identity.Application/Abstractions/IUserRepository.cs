using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface IUserRepository
{
    /// <summary>Returns the user by primary key within the current tenant scope.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Looks up a user by their normalised email (upper-case) within the current tenant.</summary>
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>Returns true if a non-deleted user with this normalised email already exists in the tenant.</summary>
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>Returns all active (non-deleted) users in the current tenant.</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Stages a new user for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a UserTenantMapping for insertion alongside a new user.
    /// Used during tenant registration to support TenantResolver fallback on subsequent logins.
    /// Caller must call SaveChangesAsync.
    /// </summary>
    Task AddTenantMappingAsync(UserTenantMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>Marks an existing user as modified. Caller must call SaveChangesAsync.</summary>
    void Update(User user);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
