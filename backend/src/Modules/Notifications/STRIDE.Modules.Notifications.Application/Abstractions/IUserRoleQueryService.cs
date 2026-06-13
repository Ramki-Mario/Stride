namespace STRIDE.Modules.Notifications.Application.Abstractions;

/// <summary>
/// Cross-module bridge to read user-role membership from the Identity schema.
/// Used by notification handlers that need to fan out to all eligible approvers
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
}
