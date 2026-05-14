using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface IUserRepository
{
    /// <summary>Returns the user by primary key within the current tenant scope.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Looks up a user by their normalised email (upper-case) within the current tenant.</summary>
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default);

    /// <summary>Returns true if a non-deleted user with this normalised email already exists in the tenant.</summary>
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct = default);

    /// <summary>Returns all active (non-deleted) users in the current tenant.</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Stages a new user for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>Marks an existing user as modified. Caller must call SaveChangesAsync.</summary>
    void Update(User user);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
