using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Enums;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Infrastructure.Persistence;

public sealed class WebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly WebhooksDbContext _ctx;

    public WebhookDeliveryRepository(WebhooksDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default) =>
        await _ctx.WebhookDeliveries.AddAsync(delivery, cancellationToken);

    public Task<WebhookDelivery?> GetByIdAsync(
        Guid tenantId, Guid deliveryId, CancellationToken cancellationToken = default) =>
        _ctx.WebhookDeliveries.FirstOrDefaultAsync(
            d => d.TenantId == tenantId && d.Id == deliveryId && !d.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<WebhookDelivery>> GetDueForRetryAsync(
        DateTime utcNow, int batchSize, CancellationToken cancellationToken = default)
    {
        return await _ctx.WebhookDeliveries
            .Where(d => d.Status == DeliveryStatus.Failed
                        && d.NextAttemptAt <= utcNow
                        && !d.IsDeleted)
            .OrderBy(d => d.NextAttemptAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public void Update(WebhookDelivery delivery) =>
        _ctx.WebhookDeliveries.Update(delivery);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ctx.SaveChangesAsync(cancellationToken);
}
