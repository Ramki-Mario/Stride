namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Cross-module bridge to read user-role membership from the Identity schema.
/// Used by notification and event handlers that need to fan out to eligible users
/// without taking a project reference to the Identity module.
/// </summary>
public interface IUserRoleQueryService
{
    Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(
        Guid roleId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active users in the tenant whose role name matches <paramref name="roleName"/>
    /// (case-insensitive). Used when a specific role Guid is not available (e.g. "Admin").
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUsersByRoleNameAsync(
        string roleName,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns email addresses of active users in the tenant whose role name matches <paramref name="roleName"/>.
    /// </summary>
    Task<IReadOnlyList<string>> GetEmailsByRoleNameAsync(
        string roleName,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
