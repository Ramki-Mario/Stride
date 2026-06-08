using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Infrastructure.Services;

/// <summary>
/// Cross-module read bridge to <c>identity.UserRoles</c> and <c>identity.Users</c>.
/// Uses Dapper (raw SQL) so that no project reference to Identity.Application is needed —
/// the Workflows module remains decoupled from the Identity module at the code level.
/// </summary>
internal sealed class UserRoleService : IUserRoleService
{
    private readonly IDbConnectionFactory _db;

    public UserRoleService(IDbConnectionFactory db) => _db = db;

    public async Task<bool> UserHasRoleAsync(
        Guid userId,
        Guid roleId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM   [identity].[UserRoles]  ur
            JOIN   [identity].[Users]      u  ON u.Id = ur.UserId
            WHERE  ur.TenantId = @TenantId
              AND  ur.UserId   = @UserId
              AND  ur.RoleId   = @RoleId
              AND  ur.IsDeleted = 0
              AND  u.IsActive   = 1
              AND  u.IsDeleted  = 0
            """;

        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { TenantId = tenantId, UserId = userId, RoleId = roleId },
                cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(
        Guid roleId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ur.UserId
            FROM   [identity].[UserRoles]  ur
            JOIN   [identity].[Users]      u  ON u.Id = ur.UserId
            WHERE  ur.TenantId = @TenantId
              AND  ur.RoleId   = @RoleId
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
}
