using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Infrastructure.Persistence;

public sealed class WebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly WebhooksDbContext _ctx;

    public WebhookSubscriptionRepository(WebhooksDbContext ctx) => _ctx = ctx;

    public Task<WebhookSubscription?> GetByIdAsync(
        Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default) =>
        _ctx.WebhookSubscriptions.FirstOrDefaultAsync(
            s => s.TenantId == tenantId && s.Id == subscriptionId && !s.IsDeleted, cancellationToken);

    public Task<int> CountActiveAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        _ctx.WebhookSubscriptions.CountAsync(
            s => s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveByEventTypeAsync(
        Guid tenantId, string eventType, CancellationToken cancellationToken = default)
    {
        // EventTypesJson is a JSON array string; we filter by substring containment as a fast
        // pre-filter, then materialise and refine in C# — the table is small per tenant and the
        // JSON column has no SQL index, so this trades a tiny over-fetch for simplicity.
        var candidates = await _ctx.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive && !s.IsDeleted
                        && s.EventTypesJson.Contains(eventType))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(s => s.EventTypes.Contains(eventType, StringComparer.Ordinal))
            .ToList();
    }

    public async Task AddAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default) =>
        await _ctx.WebhookSubscriptions.AddAsync(subscription, cancellationToken);

    public void Update(WebhookSubscription subscription) =>
        _ctx.WebhookSubscriptions.Update(subscription);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ctx.SaveChangesAsync(cancellationToken);
}
