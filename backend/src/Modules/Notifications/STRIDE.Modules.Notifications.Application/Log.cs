using Microsoft.Extensions.Logging;
using STRIDE.Modules.Notifications.Domain.Enums;

namespace STRIDE.Modules.Notifications.Application;

internal static partial class Log
{
    // ── CreateNotification ────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Notification {Id} ({Type}) created for recipient {RecipientId} in tenant {TenantId}")]
    internal static partial void NotificationCreated(
        this ILogger logger, Guid id, NotificationType type, Guid recipientId, Guid tenantId);

    // ── DeleteNotification ────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "DeleteNotification: notification {Id} not found for tenant {TenantId}")]
    internal static partial void DeleteNotificationNotFound(this ILogger logger, Guid id, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "DeleteNotification: recipient mismatch for notification {Id}")]
    internal static partial void DeleteNotificationRecipientMismatch(this ILogger logger, Guid id);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Notification {Id} soft-deleted by {RecipientId}")]
    internal static partial void NotificationDeleted(this ILogger logger, Guid id, Guid recipientId);

    // ── MarkNotificationAsRead ────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "MarkAsRead: notification {Id} not found for tenant {TenantId}")]
    internal static partial void MarkAsReadNotFound(this ILogger logger, Guid id, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "MarkAsRead: recipient mismatch for notification {Id}")]
    internal static partial void MarkAsReadRecipientMismatch(this ILogger logger, Guid id);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Notification {Id} marked as read by {RecipientId}")]
    internal static partial void NotificationMarkedAsRead(this ILogger logger, Guid id, Guid recipientId);
}
