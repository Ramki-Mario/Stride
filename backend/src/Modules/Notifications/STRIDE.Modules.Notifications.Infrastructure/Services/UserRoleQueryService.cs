using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Notifications.Application.Abstractions;

namespace STRIDE.Modules.Notifications.Infrastructure.Services;

/// <summary>
/// Cross-module Dapper bridge to <c>identity.UserRoles</c>.
/// Returns all active users that hold the given role within the tenant.
/// </summary>
internal sealed class UserRoleQueryService : IUserRoleQueryService
{
    private readonly IDbConnectionFactory _db;

    public UserRoleQueryService(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(
        Guid roleId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ur.UserId
            FROM   [identity].[UserRoles]  ur
            JOIN   [identity].[Users]      u  ON u.Id = ur.UserId
            WHERE  ur.TenantId  = @TenantId
              AND  ur.RoleId    = @RoleId
              AND  ur.IsDeleted = 0
              AND  u.IsActive   = 1
              AND  u.IsDeleted  = 0
            """;

        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var userIds = await conn.QueryAsync<Guid>(
            new CommandDefinition(sql, new { TenantId = tenantId, RoleId = roleId },
                cancellationToken: cancellationToken));

        return userIds.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<Guid>> GetUsersByRoleNameAsync(
        string roleName,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ur.UserId
            FROM   [identity].[UserRoles]  ur
            JOIN   [identity].[Users]      u  ON u.Id       = ur.UserId
            JOIN   [identity].[Roles]      r  ON r.Id       = ur.RoleId
            WHERE  ur.TenantId  = @TenantId
              AND  r.Name       = @RoleName
              AND  ur.IsDeleted = 0
              AND  u.IsActive   = 1
              AND  u.IsDeleted  = 0
            """;

        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var userIds = await conn.QueryAsync<Guid>(
            new CommandDefinition(sql, new { TenantId = tenantId, RoleName = roleName },
                cancellationToken: cancellationToken));

        return userIds.ToList().AsReadOnly();
    }
}
