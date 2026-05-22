using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Notifications.Domain.Events;

namespace STRIDE.Modules.Notifications.Domain.Entities;

public sealed class Notification : AuditableEntity
{
    public Guid RecipientId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }

    // EF Core
    private Notification() { }

    /// <summary>
    /// Factory method — the only way to create a <see cref="Notification"/>.
    /// Raises <see cref="NotificationCreatedEvent"/>.
    /// </summary>
    public static Notification Create(
        Guid tenantId,
        Guid recipientId,
        NotificationType type,
        string title,
        string body,
        Guid createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var notification = new Notification
        {
            Id          = Guid.NewGuid(),
            TenantId    = tenantId,
            RecipientId = recipientId,
            Type        = type,
            Title       = title,
            Body        = body,
            IsRead      = false,
            IsDeleted   = false,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow,
            CreatedBy   = createdBy,
        };

        notification.RaiseDomainEvent(new NotificationCreatedEvent(
            notification.Id,
            tenantId,
            recipientId,
            type,
            title));

        return notification;
    }

    /// <summary>
    /// Marks the notification as read. Idempotent — no-op if already read.
    /// </summary>
    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead    = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft-deletes the notification.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
