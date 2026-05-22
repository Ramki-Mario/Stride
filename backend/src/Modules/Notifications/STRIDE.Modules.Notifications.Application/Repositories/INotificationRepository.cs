using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Application.Repositories;

public interface INotificationRepository
{
    /// <summary>Returns all non-deleted notifications for a recipient within a tenant, newest first.</summary>
    Task<IReadOnlyList<Notification>> GetByRecipientAsync(
        Guid tenantId,
        Guid recipientId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single notification by ID, or null if not found / soft-deleted.</summary>
    Task<Notification?> GetByIdAsync(
        Guid tenantId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the count of unread notifications for a recipient within a tenant.</summary>
    Task<int> GetUnreadCountAsync(
        Guid tenantId,
        Guid recipientId,
        CancellationToken cancellationToken = default);

    /// <summary>Persists a new notification.</summary>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing notification (e.g., MarkAsRead / Delete).</summary>
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
}
