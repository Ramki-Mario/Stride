namespace STRIDE.Modules.Identity.Application.Abstractions;

/// <summary>
/// Resolves the effective set of permission keys a user holds within a tenant,
/// by walking UserRoles → RolePermissions → Permissions. Results are cached per
/// <c>(tenantId, userId)</c> with a short TTL and invalidated whenever the user's
/// role assignments change.
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Returns the distinct permission keys (e.g. <c>workflow.create</c>) granted to
    /// the user via their assigned roles. Returns an empty set if the user has none.
    /// </summary>
    Task<IReadOnlySet<string>> GetPermissionsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evicts the cached permission set for a user so the next authorization check
    /// reflects an updated role assignment. Call after granting/revoking roles.
    /// </summary>
    void InvalidateUser(Guid tenantId, Guid userId);
}
