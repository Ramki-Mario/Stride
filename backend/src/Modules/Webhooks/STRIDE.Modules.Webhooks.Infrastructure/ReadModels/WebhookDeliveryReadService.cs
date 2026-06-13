using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Application.DTOs;
using STRIDE.Modules.Webhooks.Domain.Enums;

namespace STRIDE.Modules.Webhooks.Infrastructure.ReadModels;

internal sealed class WebhookDeliveryReadService : IWebhookDeliveryReadService
{
    private static readonly string SqlGetDeliveries =
        SqlLoader.Load(typeof(WebhookDeliveryReadService).Assembly,
            "STRIDE.Modules.Webhooks.Infrastructure.ReadModels.Queries.GetWebhookDeliveries.sql");

    private readonly IDbConnectionFactory _db;

    public WebhookDeliveryReadService(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<WebhookDeliveryDto>> GetBySubscriptionAsync(
        Guid tenantId, Guid subscriptionId, int limit, CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition(
            SqlGetDeliveries,
            new { TenantId = tenantId, SubscriptionId = subscriptionId, Limit = limit },
            commandTimeout: 30,
            cancellationToken: cancellationToken));

        return rows.Select(Map).ToList().AsReadOnly();
    }

    private static WebhookDeliveryDto Map(Row row) =>
        new(row.Id,
            row.EventType,
            ((DeliveryStatus)row.Status).ToString(),
            row.ResponseCode,
            row.ResponseBody,
            row.AttemptCount,
            row.CreatedAt,
            row.LastAttemptAt,
            row.NextAttemptAt);

    private sealed record Row(
        Guid      Id,
        string    EventType,
        int       Status,
        int?      ResponseCode,
        string?   ResponseBody,
        int       AttemptCount,
        DateTime  CreatedAt,
        DateTime? LastAttemptAt,
        DateTime? NextAttemptAt);
}
