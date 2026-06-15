using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Fans out <see cref="NotificationType.ApprovalRequested"/> in-app and Web Push
/// notifications to every active user who holds the required approver role.
/// When no role is configured on the step, the event is silently ignored —
/// the approval can still be acted on via the detail page.
/// </summary>
internal sealed class ApprovalRequestedNotificationHandler
    : INotificationHandler<DomainEventNotification<ApprovalRequestedEvent>>
{
    private readonly ISender _sender;
    private readonly IUserRoleQueryService _userRoles;
    private readonly IPushSubscriptionRepository _pushSubs;
    private readonly IWebPushService _webPush;
    private readonly ILogger<ApprovalRequestedNotificationHandler> _logger;

    public ApprovalRequestedNotificationHandler(
        ISender sender,
        IUserRoleQueryService userRoles,
        IPushSubscriptionRepository pushSubs,
        IWebPushService webPush,
        ILogger<ApprovalRequestedNotificationHandler> logger)
    {
        _sender    = sender;
        _userRoles = userRoles;
        _pushSubs  = pushSubs;
        _webPush   = webPush;
        _logger    = logger;
    }

    public async Task Handle(
        DomainEventNotification<ApprovalRequestedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        if (e.RequiredRoleId is null)
            return;

        var approverIds = await _userRoles.GetUsersInRoleAsync(
            e.RequiredRoleId.Value, e.TenantId, cancellationToken);

        if (approverIds.Count == 0)
            return;

        var title = "Your approval is required";
        var body  = string.IsNullOrWhiteSpace(e.WorkflowName)
            ? $"Step '{e.StepName}' requires your approval."
            : $"Step '{e.StepName}' in workflow '{e.WorkflowName}' requires your approval.";

        foreach (var approverId in approverIds)
        {
            var command = new CreateNotificationCommand(
                TenantId:    e.TenantId,
                RecipientId: approverId,
                Type:        NotificationType.ApprovalRequested,
                Title:       title,
                Body:        body,
                CreatedBy:   Guid.Empty);

            var result = await _sender.Send(command, cancellationToken);

            if (!result.IsSuccess)
                _logger.LogWarning(
                    "Failed to create ApprovalRequested notification for step {StepId}, user {UserId}: {Error}",
                    e.StepInstanceId, approverId, result.Error);

            var subscriptions = await _pushSubs.GetByUserAsync(e.TenantId, approverId, cancellationToken);
            foreach (var sub in subscriptions)
            {
                await _webPush.SendAsync(sub, title, body, cancellationToken);
            }
        }
    }
}
