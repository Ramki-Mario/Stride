using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Webhooks.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Notifies all tenant admins when a webhook delivery is exhausted (all 4 attempts failed).
/// Follows the same fan-out pattern as <see cref="ApprovalRequestedNotificationHandler"/>.
/// </summary>
internal sealed class WebhookDeliveryExhaustedNotificationHandler
    : INotificationHandler<DomainEventNotification<WebhookDeliveryExhaustedEvent>>
{
    private const string AdminRoleName = "Admin";

    private readonly ISender                     _sender;
    private readonly IUserRoleQueryService       _userRoles;
    private readonly IPushSubscriptionRepository _pushSubs;
    private readonly IWebPushService             _webPush;
    private readonly ILogger<WebhookDeliveryExhaustedNotificationHandler> _logger;

    public WebhookDeliveryExhaustedNotificationHandler(
        ISender sender,
        IUserRoleQueryService userRoles,
        IPushSubscriptionRepository pushSubs,
        IWebPushService webPush,
        ILogger<WebhookDeliveryExhaustedNotificationHandler> logger)
    {
        _sender    = sender;
        _userRoles = userRoles;
        _pushSubs  = pushSubs;
        _webPush   = webPush;
        _logger    = logger;
    }

    public async Task Handle(
        DomainEventNotification<WebhookDeliveryExhaustedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var adminIds = await _userRoles.GetUsersByRoleNameAsync(AdminRoleName, e.TenantId, cancellationToken);
        if (adminIds.Count == 0)
            return;

        const string title = "Webhook delivery failed";
        var body = $"All delivery attempts for event '{e.EventType}' have failed. " +
                   $"Please check your webhook subscription and retry from the Webhooks page.";

        foreach (var adminId in adminIds)
        {
            var result = await _sender.Send(new CreateNotificationCommand(
                TenantId:    e.TenantId,
                RecipientId: adminId,
                Type:        NotificationType.WebhookDeliveryExhausted,
                Title:       title,
                Body:        body,
                CreatedBy:   Guid.Empty), cancellationToken);

            if (!result.IsSuccess)
                _logger.LogWarning(
                    "Failed to create WebhookDeliveryExhausted notification for delivery {DeliveryId}, user {UserId}: {Error}",
                    e.DeliveryId, adminId, result.Error);

            var subscriptions = await _pushSubs.GetByUserAsync(e.TenantId, adminId, cancellationToken);
            foreach (var sub in subscriptions)
                await _webPush.SendAsync(sub, title, body, cancellationToken);
        }
    }
}
