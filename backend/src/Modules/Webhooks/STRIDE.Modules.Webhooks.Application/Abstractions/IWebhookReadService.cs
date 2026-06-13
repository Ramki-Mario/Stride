using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Abstractions;

/// <summary>
/// Read-side queries for webhook subscriptions. Projections never include the
/// signing secret — it is shown only once on creation/regeneration via the
/// command result, and masked everywhere thereafter.
/// </summary>
public interface IWebhookReadService
{
    Task<IReadOnlyList<WebhookSubscriptionDto>> GetSubscriptionsAsync(
        Guid tenantId, CancellationToken cancellationToken = default);

    Task<WebhookSubscriptionDto?> GetSubscriptionByIdAsync(
        Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default);
}
