using STRIDE.BuildingBlocks.Domain.Events;
using STRIDE.Modules.Notifications.Domain.Enums;

namespace STRIDE.Modules.Notifications.Domain.Events;

public sealed record NotificationCreatedEvent(
    Guid NotificationId,
    Guid TenantId,
    Guid RecipientId,
    NotificationType Type,
    string Title) : IDomainEvent;
