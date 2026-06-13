using STRIDE.Modules.Webhooks.Domain.Entities;

namespace STRIDE.Modules.Webhooks.Domain.Repositories;

public interface IWebhookSubscriptionRepository
{
    Task<WebhookSubscription?> GetByIdAsync(Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<int>                  CountActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active, non-deleted subscriptions for the tenant that include
    /// <paramref name="eventType"/> in their subscribed set. Used by the dispatch
    /// pipeline to fan out a domain event to matching endpoints.
    /// Returns entities with <c>SigningSecret</c> already decrypted by the EF value converter.
    /// </summary>
    Task<IReadOnlyList<WebhookSubscription>> GetActiveByEventTypeAsync(
        Guid tenantId, string eventType, CancellationToken cancellationToken = default);

    Task AddAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);
    void Update(WebhookSubscription subscription);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
