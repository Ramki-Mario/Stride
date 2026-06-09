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
}
