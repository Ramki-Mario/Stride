using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.StepAssigned"/> in-app notification for the assignee
/// and sends a Web Push notification if they have registered push subscriptions.
/// </summary>
internal sealed class StepAssignedNotificationHandler
    : INotificationHandler<DomainEventNotification<StepAssignedEvent>>
{
    private readonly ISender _sender;
    private readonly IPushSubscriptionRepository _pushSubs;
    private readonly IWebPushService _webPush;
    private readonly ILogger<StepAssignedNotificationHandler> _logger;

    public StepAssignedNotificationHandler(
        ISender sender,
        IPushSubscriptionRepository pushSubs,
        IWebPushService webPush,
        ILogger<StepAssignedNotificationHandler> logger)
    {
        _sender   = sender;
        _pushSubs = pushSubs;
        _webPush  = webPush;
        _logger   = logger;
    }

    public async Task Handle(
        DomainEventNotification<StepAssignedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        const string title = "A step has been assigned to you";
        const string body  = "You have been assigned a workflow step. Please review and complete it.";

        // ── In-app notification ────────────────────────────────────────────────
        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.AssigneeId,
            Type:        NotificationType.StepAssigned,
            Title:       title,
            Body:        body,
            CreatedBy:   e.AssignedBy);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create StepAssigned notification for step {StepId}: {Error}",
                e.StepInstanceId, result.Error);

        // ── Web Push ───────────────────────────────────────────────────────────
        var subscriptions = await _pushSubs.GetByUserAsync(e.TenantId, e.AssigneeId, cancellationToken);
        foreach (var sub in subscriptions)
        {
            await _webPush.SendAsync(sub, title, body, cancellationToken);
        }
    }
}
