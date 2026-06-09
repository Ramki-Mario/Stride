using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Application.Repositories;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetByUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PushSubscription?> GetByEndpointAsync(
        Guid tenantId,
        string endpoint,
        CancellationToken cancellationToken = default);

    Task AddAsync(PushSubscription subscription, CancellationToken cancellationToken = default);

    Task RemoveAsync(PushSubscription subscription, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
