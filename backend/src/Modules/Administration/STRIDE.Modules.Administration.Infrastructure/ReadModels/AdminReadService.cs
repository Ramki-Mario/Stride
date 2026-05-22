using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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

    private readonly string _connectionString;

    public AdminReadService(IConfiguration configuration)
        => _connectionString = configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
        Guid tenantId,
        string? search,
        string? role,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var offset = (page - 1) * pageSize;
        var param = new
        {
            TenantId = tenantId,
            Search   = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Role     = string.IsNullOrWhiteSpace(role)   ? null : role.Trim(),
            Status   = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            Offset   = offset,
            PageSize = pageSize
        };

        await using var conn = new SqlConnection(_connectionString);

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(SqlCountUsers, param, cancellationToken: ct));

        if (total == 0)
            return PagedResult<AdminUserDto>.Empty(page, pageSize);

        var items = await conn.QueryAsync<AdminUserDto>(
            new CommandDefinition(SqlGetUsers, param, cancellationToken: ct));

        return new PagedResult<AdminUserDto>(items.AsList(), total, page, pageSize);
    }
}
