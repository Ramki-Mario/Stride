using Dapper;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Infrastructure.ReadModels;

internal sealed class KitItemReadService : IKitItemReadService
{
    private static readonly string SqlGetKitItems =
        SqlLoader.Load(typeof(KitItemReadService).Assembly,
            "STRIDE.Modules.KitOps.Infrastructure.ReadModels.Queries.GetKitItems.sql");

    private static readonly string SqlCountKitItems =
        SqlLoader.Load(typeof(KitItemReadService).Assembly,
            "STRIDE.Modules.KitOps.Infrastructure.ReadModels.Queries.CountKitItems.sql");

    private static readonly string SqlGetById =
        SqlLoader.Load(typeof(KitItemReadService).Assembly,
            "STRIDE.Modules.KitOps.Infrastructure.ReadModels.Queries.GetKitItemById.sql");

    private readonly IDbConnectionFactory _db;

    public KitItemReadService(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<KitItemSummaryDto>> GetKitItemsAsync(
        Guid tenantId, string? search, string? category, bool? isActive,
        int page, int pageSize, CancellationToken ct = default)
    {
        var param = new
        {
            TenantId = tenantId,
            Search   = string.IsNullOrWhiteSpace(search)   ? null : search.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            IsActive = isActive.HasValue ? (object)isActive.Value : null,
            Offset   = (page - 1) * pageSize,
            PageSize = pageSize,
        };

        await using var conn = await _db.OpenConnectionAsync(ct);

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(SqlCountKitItems, param, cancellationToken: ct));

        if (total == 0)
            return PagedResult<KitItemSummaryDto>.Empty(page, pageSize);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlGetKitItems, param, cancellationToken: ct));

        var items = rows.Select(r => new KitItemSummaryDto(
            Id:            (Guid)r.Id,
            Name:          (string)r.Name,
            Category:      (string)r.Category,
            Description:   r.Description is DBNull ? null : (string?)r.Description,
            TotalQuantity: (int)r.TotalQuantity,
            IsActive:      (bool)r.IsActive,
            CreatedAt:     (DateTime)r.CreatedAt
        )).ToList();

        return new PagedResult<KitItemSummaryDto>(items, total, page, pageSize);
    }

    public async Task<KitItemDetailDto?> GetKitItemByIdAsync(
        Guid tenantId, Guid kitItemId, CancellationToken ct = default)
    {
        var param = new { TenantId = tenantId, KitItemId = kitItemId };

        await using var conn = await _db.OpenConnectionAsync(ct);

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(SqlGetById, param, cancellationToken: ct));

        if (row is null) return null;

        return new KitItemDetailDto(
            Id:            (Guid)row.Id,
            Name:          (string)row.Name,
            Category:      (string)row.Category,
            Description:   row.Description is DBNull ? null : (string?)row.Description,
            TotalQuantity: (int)row.TotalQuantity,
            IsActive:      (bool)row.IsActive,
            CreatedAt:     (DateTime)row.CreatedAt,
            UpdatedAt:     (DateTime)row.UpdatedAt);
    }
}
