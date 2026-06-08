namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Cross-module bridge: allows the Workflows application layer to query whether
/// a user holds a given Identity role, without creating a hard project reference
/// to the Identity module. Implemented in Infrastructure via Dapper.
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Returns true if the specified user is active and holds the given role within the tenant.
    /// </summary>
    Task<bool> UserHasRoleAsync(
        Guid userId,
        Guid roleId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the user IDs of all active users who hold the given role within the tenant.
    /// Used for auto-assignment when exactly one user holds a required role.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(
        Guid roleId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
