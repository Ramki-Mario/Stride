using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using STRIDE.BFF.Hubs;
using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.BFF.Realtime;

/// <summary>
/// Bridges Redis pub/sub to SignalR (US-176).
///
/// The Host publishes <see cref="DashboardUpdateMessage"/> payloads on
/// <see cref="DashboardUpdates.Channel"/> whenever a dashboard-relevant domain
/// event fires; this hosted service forwards each message to the matching
/// tenant group on <see cref="DashboardHub"/>. Redis pub/sub callbacks arrive
/// on the multiplexer's thread — per-message failures are logged and swallowed
/// so one malformed payload cannot kill the subscription.
/// </summary>
public sealed class DashboardRedisSubscriber : IHostedService
{
    /// <summary>Client-side handler name, received by the Angular DashboardRealtimeService.</summary>
    public const string ClientMethod = "dashboardUpdate";

    private readonly IConnectionMultiplexer             _redis;
    private readonly IHubContext<DashboardHub>          _hub;
    private readonly ILogger<DashboardRedisSubscriber>  _logger;

    public DashboardRedisSubscriber(
        IConnectionMultiplexer redis,
        IHubContext<DashboardHub> hub,
        ILogger<DashboardRedisSubscriber> logger)
    {
        _redis  = redis;
        _hub    = hub;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _redis.GetSubscriber().SubscribeAsync(
            RedisChannel.Literal(DashboardUpdates.Channel),
            (channel, value) => _ = ForwardAsync(value));

        _logger.LogInformation(
            "Dashboard realtime bridge subscribed to Redis channel {Channel}.",
            DashboardUpdates.Channel);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _redis.GetSubscriber().UnsubscribeAsync(RedisChannel.Literal(DashboardUpdates.Channel));

    private async Task ForwardAsync(RedisValue value)
    {
        try
        {
            var message = JsonSerializer.Deserialize<DashboardUpdateMessage>(value.ToString());
            if (message is null || message.TenantId == Guid.Empty) return;

            await _hub.Clients
                .Group(DashboardHub.TenantGroup(message.TenantId))
                .SendAsync(ClientMethod, message.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to forward dashboard update: {Payload}", value.ToString());
        }
    }
}
