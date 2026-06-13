using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STRIDE.Modules.Webhooks.Domain.Repositories;
using STRIDE.Modules.Webhooks.Infrastructure.Dispatch;

namespace STRIDE.Modules.Webhooks.Infrastructure.BackgroundServices;

/// <summary>
/// Scans for Failed webhook deliveries that are due for retry every 30 seconds and
/// re-attempts their HTTP dispatch. Each retry uses the same signing key as the original
/// attempt by loading the subscription from the repository (so secret rotation is picked up).
///
/// The service intentionally processes up to 50 deliveries per tick to avoid unbounded
/// DB queries. In practice the queue will be tiny for the expected tenant counts.
/// </summary>
internal sealed class WebhookRetryService : BackgroundService
{
    private static readonly TimeSpan TickInterval  = TimeSpan.FromSeconds(30);
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory            _scopeFactory;
    private readonly ILogger<WebhookRetryService>    _logger;

    public WebhookRetryService(IServiceScopeFactory scopeFactory, ILogger<WebhookRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessDueRetriesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error during webhook retry scan");
            }
        }
    }

    private async Task ProcessDueRetriesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var deliveryRepo     = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryRepository>();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<IWebhookSubscriptionRepository>();
        var dispatcher       = scope.ServiceProvider.GetRequiredService<HttpWebhookDispatcher>();

        var due = await deliveryRepo.GetDueForRetryAsync(DateTime.UtcNow, BatchSize, cancellationToken);

        if (due.Count == 0)
            return;

        _logger.LogInformation("Webhook retry scan: {Count} deliveries due for retry", due.Count);

        foreach (var delivery in due)
        {
            var sub = await subscriptionRepo.GetByIdAsync(
                delivery.TenantId, delivery.WebhookSubscriptionId, cancellationToken);

            if (sub is null || !sub.IsActive)
            {
                _logger.LogWarning(
                    "Delivery {DeliveryId}: subscription {SubscriptionId} not found or inactive — skipping retry",
                    delivery.Id, delivery.WebhookSubscriptionId);
                continue;
            }

            await dispatcher.AttemptDeliveryAsync(delivery, sub.SigningSecret, cancellationToken);
        }
    }
}
