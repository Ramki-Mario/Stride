using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Infrastructure.Persistence.Repositories;

internal sealed class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly NotificationsDbContext _context;

    public PushSubscriptionRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PushSubscription>> GetByUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.PushSubscriptions
            .Where(s => s.TenantId == tenantId && s.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<PushSubscription?> GetByEndpointAsync(
        Guid tenantId,
        string endpoint,
        CancellationToken cancellationToken = default)
        => await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Endpoint == endpoint, cancellationToken);

    public async Task AddAsync(PushSubscription subscription, CancellationToken cancellationToken = default)
        => await _context.PushSubscriptions.AddAsync(subscription, cancellationToken);

    public Task RemoveAsync(PushSubscription subscription, CancellationToken cancellationToken = default)
    {
        _context.PushSubscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
