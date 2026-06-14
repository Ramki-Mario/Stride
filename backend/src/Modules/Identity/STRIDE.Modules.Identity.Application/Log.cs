using Microsoft.Extensions.Logging;

namespace STRIDE.Modules.Identity.Application;

internal static partial class Log
{
    // ── AssignRole ─────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning, Message = "AssignRole failed: user {UserId} not found")]
    internal static partial void AssignRoleUserNotFound(this ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AssignRole failed: role {RoleId} not found")]
    internal static partial void AssignRoleRoleNotFound(this ILogger logger, Guid roleId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Role {RoleName} assigned to user {UserId} by {AssignedBy}")]
    internal static partial void RoleAssigned(this ILogger logger, string roleName, Guid userId, Guid assignedBy);

    // ── Login ──────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed: tenant not found for {Email}")]
    internal static partial void LoginTenantNotFound(this ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed: user not found or inactive for {Email}")]
    internal static partial void LoginUserNotFound(this ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed: incorrect password for {Email}")]
    internal static partial void LoginIncorrectPassword(this ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "User {UserId} in tenant {TenantId} logged in successfully")]
    internal static partial void LoginSucceeded(this ILogger logger, Guid userId, Guid tenantId);

    // ── RefreshToken ───────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning, Message = "Token refresh failed: token is invalid, expired, or revoked")]
    internal static partial void RefreshTokenInvalid(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Token refresh failed: user {UserId} not found or inactive")]
    internal static partial void RefreshTokenUserNotFound(this ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Refresh token rotated for user {UserId} in tenant {TenantId}")]
    internal static partial void RefreshTokenRotated(this ILogger logger, Guid userId, Guid tenantId);

    // ── RevokeToken ────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning, Message = "Token revocation failed: token not found")]
    internal static partial void RevokeTokenNotFound(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Refresh token revoked for user {UserId} in tenant {TenantId}")]
    internal static partial void RevokeTokenSucceeded(this ILogger logger, Guid userId, Guid tenantId);

    // ── RegisterTenant ─────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "[RegisterTenant] Tenant {TenantId} ('{Slug}') created with admin user {UserId}")]
    internal static partial void TenantRegistered(this ILogger logger, Guid tenantId, string slug, Guid userId);

    // ── RegisterUser ───────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Registration failed: no tenant found for email domain of {Email}")]
    internal static partial void RegisterTenantNotFound(this ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Registration failed: email {Email} already exists in tenant {TenantId}")]
    internal static partial void RegisterEmailExists(this ILogger logger, string email, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "User {UserId} registered in tenant {TenantId}")]
    internal static partial void UserRegistered(this ILogger logger, Guid userId, Guid tenantId);
}
