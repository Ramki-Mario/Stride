using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.StepAssigned"/> notification for the assignee
/// whenever a <see cref="StepAssignedEvent"/> is dispatched.
/// </summary>
internal sealed class StepAssignedNotificationHandler
    : INotificationHandler<DomainEventNotification<StepAssignedEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<StepAssignedNotificationHandler> _logger;

    public StepAssignedNotificationHandler(
        ISender sender,
        ILogger<StepAssignedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<StepAssignedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.AssigneeId,
            Type:        NotificationType.StepAssigned,
            Title:       "A step has been assigned to you",
            Body:        "You have been assigned a workflow step. Please review and complete it.",
            CreatedBy:   e.AssignedBy);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create StepAssigned notification for step {StepId}: {Error}",
                e.StepInstanceId, result.Error);
    }
}
