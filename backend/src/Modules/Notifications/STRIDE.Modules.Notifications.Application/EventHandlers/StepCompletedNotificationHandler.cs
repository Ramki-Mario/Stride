using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.StepCompleted"/> notification for the user who
/// completed the step whenever a <see cref="StepCompletedEvent"/> is dispatched.
/// </summary>
internal sealed class StepCompletedNotificationHandler
    : INotificationHandler<DomainEventNotification<StepCompletedEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<StepCompletedNotificationHandler> _logger;

    public StepCompletedNotificationHandler(
        ISender sender,
        ILogger<StepCompletedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<StepCompletedEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.CompletedBy,
            Type:        NotificationType.StepCompleted,
            Title:       "Step completed",
            Body:        "A workflow step has been marked as completed.",
            CreatedBy:   e.CompletedBy);

        var result = await _sender.Send(command, ct);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create StepCompleted notification for step {StepId}: {Error}",
                e.StepInstanceId, result.Error);
    }
}
