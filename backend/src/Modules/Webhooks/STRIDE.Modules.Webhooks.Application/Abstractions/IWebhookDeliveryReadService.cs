using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Abstractions;

public interface IWebhookDeliveryReadService
{
    Task<IReadOnlyList<WebhookDeliveryDto>> GetBySubscriptionAsync(
        Guid tenantId, Guid subscriptionId, int limit, CancellationToken cancellationToken = default);
}
