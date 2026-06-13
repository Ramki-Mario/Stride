using STRIDE.Modules.Webhooks.Domain.Entities;

namespace STRIDE.Modules.Webhooks.Domain.Repositories;

public interface IWebhookDeliveryRepository
{
    Task AddAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);
    Task<WebhookDelivery?> GetByIdAsync(Guid tenantId, Guid deliveryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns deliveries that are due for a retry: status is Failed and
    /// <c>NextAttemptAt &lt;= utcNow</c>. Called by the background retry service.
    /// </summary>
    Task<IReadOnlyList<WebhookDelivery>> GetDueForRetryAsync(DateTime utcNow, int batchSize, CancellationToken cancellationToken = default);

    void Update(WebhookDelivery delivery);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
