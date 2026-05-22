using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.WorkflowStarted"/> notification for the user who
/// triggered the workflow whenever a <see cref="WorkflowStartedEvent"/> is dispatched.
/// </summary>
internal sealed class WorkflowStartedNotificationHandler
    : INotificationHandler<DomainEventNotification<WorkflowStartedEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<WorkflowStartedNotificationHandler> _logger;

    public WorkflowStartedNotificationHandler(
        ISender sender,
        ILogger<WorkflowStartedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<WorkflowStartedEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.StartedBy,
            Type:        NotificationType.WorkflowStarted,
            Title:       $"Workflow '{e.WorkflowName}' started",
            Body:        $"Workflow '{e.WorkflowName}' has been started successfully.",
            CreatedBy:   e.StartedBy);

        var result = await _sender.Send(command, ct);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create WorkflowStarted notification for instance {Id}: {Error}",
                e.WorkflowInstanceId, result.Error);
    }
}
