using System.Text.Json;
using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Infrastructure.ReadModels;

/// <summary>
/// Dapper read-side for webhook subscriptions. SQL lives in embedded .sql files.
/// The signing secret is never selected, so it cannot leak through a read path.
/// </summary>
internal sealed class WebhookReadService : IWebhookReadService
{
    private static readonly string SqlGetSubscriptions =
        SqlLoader.Load(typeof(WebhookReadService).Assembly,
            "STRIDE.Modules.Webhooks.Infrastructure.ReadModels.Queries.GetWebhookSubscriptions.sql");

    private static readonly string SqlGetSubscriptionById =
        SqlLoader.Load(typeof(WebhookReadService).Assembly,
            "STRIDE.Modules.Webhooks.Infrastructure.ReadModels.Queries.GetWebhookSubscriptionById.sql");

    private readonly IDbConnectionFactory _db;

    public WebhookReadService(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<WebhookSubscriptionDto>> GetSubscriptionsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition(
            SqlGetSubscriptions, new { TenantId = tenantId },
            commandTimeout: 30, cancellationToken: cancellationToken));

        return rows.Select(Map).ToList().AsReadOnly();
    }

    public async Task<WebhookSubscriptionDto?> GetSubscriptionByIdAsync(
        Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(
            SqlGetSubscriptionById, new { TenantId = tenantId, SubscriptionId = subscriptionId },
            commandTimeout: 30, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    private static WebhookSubscriptionDto Map(Row row)
    {
        IReadOnlyList<string> eventTypes = string.IsNullOrWhiteSpace(row.EventTypesJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<List<string>>(row.EventTypesJson) ?? new List<string>();

        return new WebhookSubscriptionDto(row.Id, row.Url, eventTypes, row.IsActive, row.CreatedAt);
    }

    private sealed record Row(Guid Id, string Url, string EventTypesJson, bool IsActive, DateTime CreatedAt);
}
