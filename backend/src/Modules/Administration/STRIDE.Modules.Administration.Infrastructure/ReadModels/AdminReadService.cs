using Dapper;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Application.Queries.GetUsers;

namespace STRIDE.Modules.Administration.Infrastructure.ReadModels;

/// <summary>
/// Dapper-based read service for the Administration module.
/// Queries the <c>identity.*</c> schema (cross-schema read, same database).
/// </summary>
internal sealed class AdminReadService : IAdminReadService
{
    private static readonly string SqlGetUsers =
        SqlLoader.Load(typeof(AdminReadService).Assembly,
            "STRIDE.Modules.Administration.Infrastructure.ReadModels.Queries.GetUsers.sql");

    private static readonly string SqlCountUsers =
        SqlLoader.Load(typeof(AdminReadService).Assembly,
            "STRIDE.Modules.Administration.Infrastructure.ReadModels.Queries.CountUsers.sql");

    private readonly IDbConnectionFactory _db;

    public AdminReadService(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
        Guid tenantId,
        string? search,
        string? role,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;
        var param  = new
        {
            TenantId = tenantId,
            Search   = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Role     = string.IsNullOrWhiteSpace(role)   ? null : role.Trim(),
            Status   = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            Offset   = offset,
            PageSize = pageSize,
        };

        await using var conn = await _db.OpenConnectionAsync(cancellationToken);

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(SqlCountUsers, param, cancellationToken: cancellationToken));

        if (total == 0)
            return PagedResult<AdminUserDto>.Empty(page, pageSize);

        var items = await conn.QueryAsync<AdminUserDto>(
            new CommandDefinition(SqlGetUsers, param, cancellationToken: cancellationToken));

        return new PagedResult<AdminUserDto>(items.AsList(), total, page, pageSize);
    }
}
