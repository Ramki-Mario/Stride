using Dapper;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Application.Queries.GetAuditLog;

namespace STRIDE.Modules.Administration.Infrastructure.ReadModels;

internal sealed class AuditLogReadService : IAuditLogReadService
{
    private static readonly string SqlGet =
        SqlLoader.Load(typeof(AuditLogReadService).Assembly,
            "STRIDE.Modules.Administration.Infrastructure.ReadModels.Queries.GetAuditLog.sql");

    private static readonly string SqlCount =
        SqlLoader.Load(typeof(AuditLogReadService).Assembly,
            "STRIDE.Modules.Administration.Infrastructure.ReadModels.Queries.CountAuditLog.sql");

    private readonly IDbConnectionFactory _db;

    public AuditLogReadService(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<AuditLogEntryDto>> GetAuditLogAsync(
        Guid      tenantId,
        int       page,
        int       pageSize,
        DateTime? from   = null,
        DateTime? to     = null,
        string?   action = null,
        CancellationToken ct = default)
    {
        var param = new
        {
            TenantId = tenantId,
            Action   = action,
            From     = from,
            To       = to,
            Offset   = (page - 1) * pageSize,
            PageSize = pageSize,
        };

        await using var conn = await _db.OpenConnectionAsync(ct);

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(SqlCount, param, cancellationToken: ct));

        if (total == 0)
            return PagedResult<AuditLogEntryDto>.Empty(page, pageSize);

        var items = await conn.QueryAsync<AuditLogEntryDto>(
            new CommandDefinition(SqlGet, param, cancellationToken: ct));

        return new PagedResult<AuditLogEntryDto>(items.AsList(), total, page, pageSize);
    }
}
