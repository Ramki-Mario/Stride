using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.WorkflowFailed"/> notification for the user involved
/// whenever a <see cref="WorkflowFailedEvent"/> is dispatched.
/// </summary>
internal sealed class WorkflowFailedNotificationHandler
    : INotificationHandler<DomainEventNotification<WorkflowFailedEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<WorkflowFailedNotificationHandler> _logger;

    public WorkflowFailedNotificationHandler(
        ISender sender,
        ILogger<WorkflowFailedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<WorkflowFailedEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.FailedBy,
            Type:        NotificationType.WorkflowFailed,
            Title:       "Workflow failed",
            Body:        $"A workflow has failed. Reason: {e.Reason}",
            CreatedBy:   e.FailedBy);

        var result = await _sender.Send(command, ct);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create WorkflowFailed notification for instance {Id}: {Error}",
                e.WorkflowInstanceId, result.Error);
    }
}
