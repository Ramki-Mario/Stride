using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.WorkflowSlaBreached"/> notification whenever a
/// <see cref="WorkflowSlaBreachedEvent"/> is dispatched by the deadline-checker background job.
/// The notification is sent to the workflow initiator (<c>StartedBy</c>).
/// </summary>
internal sealed class WorkflowSlaBreachedNotificationHandler
    : INotificationHandler<DomainEventNotification<WorkflowSlaBreachedEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<WorkflowSlaBreachedNotificationHandler> _logger;

    public WorkflowSlaBreachedNotificationHandler(
        ISender sender,
        ILogger<WorkflowSlaBreachedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<WorkflowSlaBreachedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var deadlineFormatted = e.DeadlineAt.ToString("dd MMM yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.StartedBy,
            Type:        NotificationType.WorkflowSlaBreached,
            Title:       "Workflow SLA breached",
            Body:        $"A workflow instance has exceeded its SLA deadline ({deadlineFormatted} UTC). Please take action.",
            CreatedBy:   e.StartedBy);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create WorkflowSlaBreached notification for instance {InstanceId}: {Error}",
                e.WorkflowInstanceId, result.Error);
    }
}
