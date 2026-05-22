using STRIDE.Modules.Notifications.Domain.Enums;

namespace STRIDE.Modules.Notifications.Application.Queries.GetNotifications;

public sealed record NotificationDto(
    Guid Id,
    Guid TenantId,
    Guid RecipientId,
    NotificationType Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt);
