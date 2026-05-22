using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository
    : TenantAwareRepository<Notification, NotificationsDbContext>,
      INotificationRepository
{
    public NotificationRepository(NotificationsDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<IReadOnlyList<Notification>> GetByRecipientAsync(
        Guid tenantId,
        Guid recipientId,
        CancellationToken cancellationToken = default)
        => await Query
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Notification?> GetByIdAsync(
        Guid tenantId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
        => await Query
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

    public async Task<int> GetUnreadCountAsync(
        Guid tenantId,
        Guid recipientId,
        CancellationToken cancellationToken = default)
        => await Query
            .CountAsync(n => n.RecipientId == recipientId && !n.IsRead, cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await Context.Notifications.AddAsync(notification, cancellationToken);

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        Context.Notifications.Update(notification);
        return Task.CompletedTask;
    }
}
