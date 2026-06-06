using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.WorkflowCompleted"/> notification for the workflow
/// initiator whenever a <see cref="WorkflowCompletedEvent"/> is dispatched.
/// </summary>
internal sealed class WorkflowCompletedNotificationHandler
    : INotificationHandler<DomainEventNotification<WorkflowCompletedEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<WorkflowCompletedNotificationHandler> _logger;

    public WorkflowCompletedNotificationHandler(
        ISender sender,
        ILogger<WorkflowCompletedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<WorkflowCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.StartedBy,
            Type:        NotificationType.WorkflowCompleted,
            Title:       "Workflow completed",
            Body:        "Your workflow has been completed successfully. All steps are done.",
            CreatedBy:   e.StartedBy);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create WorkflowCompleted notification for instance {Id}: {Error}",
                e.WorkflowInstanceId, result.Error);
    }
}
