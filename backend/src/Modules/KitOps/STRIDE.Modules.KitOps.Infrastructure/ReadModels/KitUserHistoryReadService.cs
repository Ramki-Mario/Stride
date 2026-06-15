using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Infrastructure.ReadModels;

internal sealed class KitUserHistoryReadService : IKitUserHistoryReadService
{
    private static readonly string SqlMyCheckouts =
        SqlLoader.Load(typeof(KitUserHistoryReadService).Assembly,
            "STRIDE.Modules.KitOps.Infrastructure.ReadModels.Queries.GetMyCheckouts.sql");

    private static readonly string SqlMyReservations =
        SqlLoader.Load(typeof(KitUserHistoryReadService).Assembly,
            "STRIDE.Modules.KitOps.Infrastructure.ReadModels.Queries.GetMyReservations.sql");

    private static readonly string SqlCatalogWithAvailability =
        SqlLoader.Load(typeof(KitUserHistoryReadService).Assembly,
            "STRIDE.Modules.KitOps.Infrastructure.ReadModels.Queries.GetKitCatalogWithAvailability.sql");

    private readonly IDbConnectionFactory _db;

    public KitUserHistoryReadService(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<MyKitCheckoutDto>> GetMyCheckoutsAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var param = new { TenantId = tenantId, UserId = userId };
        await using var conn = await _db.OpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlMyCheckouts, param, cancellationToken: ct));

        return rows.Select(r => new MyKitCheckoutDto(
            CheckoutId:      (Guid)r.CheckoutId,
            KitItemId:       (Guid)r.KitItemId,
            KitItemName:     (string)r.KitItemName,
            Category:        (string)r.Category,
            CheckedOutAt:    (DateTime)r.CheckedOutAt,
            ExpectedReturnAt:(DateTime)r.ExpectedReturnAt,
            ReturnedAt:      r.ReturnedAt is DBNull ? null : (DateTime?)r.ReturnedAt,
            Notes:           r.Notes is DBNull ? null : (string?)r.Notes,
            StatusLabel:     (string)r.StatusLabel,
            StatusValue:     (int)r.StatusValue
        )).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<MyKitReservationDto>> GetMyReservationsAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var param = new { TenantId = tenantId, UserId = userId };
        await using var conn = await _db.OpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlMyReservations, param, cancellationToken: ct));

        return rows.Select(r => new MyKitReservationDto(
            ReservationId: (Guid)r.ReservationId,
            KitItemId:     (Guid)r.KitItemId,
            KitItemName:   (string)r.KitItemName,
            Category:      (string)r.Category,
            RequestedAt:   (DateTime)r.RequestedAt,
            Notes:         r.Notes is DBNull ? null : (string?)r.Notes,
            StatusLabel:   (string)r.StatusLabel,
            StatusValue:   (int)r.StatusValue
        )).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<KitCatalogItemDto>> GetCatalogWithAvailabilityAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var param = new { TenantId = tenantId };
        await using var conn = await _db.OpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlCatalogWithAvailability, param, cancellationToken: ct));

        return rows.Select(r => new KitCatalogItemDto(
            KitItemId:            (Guid)r.KitItemId,
            Name:                 (string)r.Name,
            Category:             (string)r.Category,
            Description:          r.Description is DBNull ? null : (string?)r.Description,
            TotalQuantity:        (int)r.TotalQuantity,
            OutstandingCheckouts: (int)r.OutstandingCheckouts,
            AvailableQuantity:    (int)r.AvailableQuantity,
            OverdueCheckouts:     (int)r.OverdueCheckouts
        )).ToList().AsReadOnly();
    }
}
