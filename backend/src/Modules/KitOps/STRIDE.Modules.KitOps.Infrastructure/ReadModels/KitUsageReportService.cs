using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Infrastructure.ReadModels;

internal sealed class KitUsageReportService : IKitUsageReportService
{
    private static readonly string SqlCheckoutHistory =
        SqlLoader.Load(typeof(KitUsageReportService).Assembly,
            "STRIDE.Modules.KitOps.Infrastructure.ReadModels.Queries.GetCheckoutHistory.sql");

    private static readonly string SqlUsageSummary =
        SqlLoader.Load(typeof(KitUsageReportService).Assembly,
            "STRIDE.Modules.KitOps.Infrastructure.ReadModels.Queries.GetUsageSummary.sql");

    private readonly IDbConnectionFactory _db;

    public KitUsageReportService(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<KitCheckoutReportRowDto>> GetCheckoutHistoryAsync(
        Guid tenantId, DateTime? from, DateTime? to, Guid? kitItemId, CancellationToken ct = default)
    {
        var param = new
        {
            TenantId  = tenantId,
            From      = from,
            To        = to,
            KitItemId = kitItemId,
        };

        await using var conn = await _db.OpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlCheckoutHistory, param, cancellationToken: ct));

        return rows.Select(r => new KitCheckoutReportRowDto(
            CheckoutId:         (Guid)r.CheckoutId,
            KitItemName:        (string)r.KitItemName,
            Category:           (string)r.Category,
            CheckedOutByUserId: (Guid)r.CheckedOutByUserId,
            CheckedOutByEmail:  r.CheckedOutByEmail is DBNull ? null : (string?)r.CheckedOutByEmail,
            CheckedOutAt:       (DateTime)r.CheckedOutAt,
            ExpectedReturnAt:   (DateTime)r.ExpectedReturnAt,
            ReturnedAt:         r.ReturnedAt is DBNull ? null : (DateTime?)r.ReturnedAt,
            Status:             (string)r.Status,
            DaysCheckedOut:     (int)r.DaysCheckedOut
        )).ToList();
    }

    public async Task<IReadOnlyList<KitUsageSummaryRowDto>> GetUsageSummaryAsync(
        Guid tenantId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var param = new { TenantId = tenantId, From = from, To = to };

        await using var conn = await _db.OpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlUsageSummary, param, cancellationToken: ct));

        return rows.Select(r => new KitUsageSummaryRowDto(
            KitItemId:          (Guid)r.KitItemId,
            Name:               (string)r.Name,
            Category:           (string)r.Category,
            TotalQuantity:      (int)r.TotalQuantity,
            TotalCheckouts:     (int)r.TotalCheckouts,
            ActiveCheckouts:    (int)r.ActiveCheckouts,
            OverdueCheckouts:   (int)r.OverdueCheckouts,
            PendingReservations:(int)r.PendingReservations,
            AvgDaysCheckedOut:  r.AvgDaysCheckedOut is DBNull ? null : (double?)r.AvgDaysCheckedOut
        )).ToList();
    }
}
