using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.StepOverdue"/> notification whenever a
/// <see cref="StepOverdueEvent"/> is dispatched by the deadline-checker background job.
/// The notification is sent to the step's assignee if one exists, otherwise to the
/// workflow initiator.
/// </summary>
internal sealed class StepOverdueNotificationHandler
    : INotificationHandler<DomainEventNotification<StepOverdueEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<StepOverdueNotificationHandler> _logger;

    public StepOverdueNotificationHandler(
        ISender sender,
        ILogger<StepOverdueNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<StepOverdueEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        // Notify the assignee if the step is assigned, otherwise fall back to the workflow starter.
        var recipientId = e.AssigneeId ?? e.StartedBy;

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: recipientId,
            Type:        NotificationType.StepOverdue,
            Title:       "Step is overdue",
            Body:        "A workflow step has passed its due date. Please review and complete it as soon as possible.",
            CreatedBy:   e.StartedBy);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create StepOverdue notification for step {StepId}: {Error}",
                e.StepInstanceId, result.Error);
    }
}
