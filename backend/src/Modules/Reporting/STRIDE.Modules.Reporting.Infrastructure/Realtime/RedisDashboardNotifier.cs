using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Abstractions;

namespace STRIDE.Modules.Reporting.Infrastructure.Realtime;

/// <summary>
/// Publishes dashboard update messages to the Redis pub/sub channel consumed by
/// the BFF's SignalR bridge (US-176).
///
/// Fire-and-forget safe: all failures are caught and logged — a Redis outage must
/// never roll back the business operation whose domain event triggered the push
/// (same resilience contract as <c>DapperAuditLogger</c>).
/// </summary>
internal sealed class RedisDashboardNotifier : IDashboardNotifier
{
    private readonly IConnectionMultiplexer          _redis;
    private readonly ILogger<RedisDashboardNotifier> _logger;

    public RedisDashboardNotifier(
        IConnectionMultiplexer redis,
        ILogger<RedisDashboardNotifier> logger)
    {
        _redis  = redis;
        _logger = logger;
    }

    public async Task PublishAsync(Guid tenantId, string eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new DashboardUpdateMessage(tenantId, eventType));

            await _redis.GetSubscriber().PublishAsync(
                RedisChannel.Literal(DashboardUpdates.Channel),
                payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Dashboard update publish failed for tenant {TenantId} ({EventType}) — clients will catch up on next poll/refresh.",
                tenantId, eventType);
        }
    }
}
