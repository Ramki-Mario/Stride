using STRIDE.Modules.Webhooks.Domain.Entities;

namespace STRIDE.Modules.Webhooks.Domain.Repositories;

public interface IWebhookSubscriptionRepository
{
    Task<WebhookSubscription?> GetByIdAsync(Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<int>                  CountActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task                       AddAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);
    void                       Update(WebhookSubscription subscription);
    Task                       SaveChangesAsync(CancellationToken cancellationToken = default);
}
