using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates an <see cref="NotificationType.InvoiceDraftCreated"/> notification for the workflow
/// initiator whenever a workflow instance completes and an invoice draft would be auto-generated.
/// Fires on the same <see cref="WorkflowCompletedEvent"/> as
/// <see cref="WorkflowCompletedNotificationHandler"/>.
/// </summary>
internal sealed class InvoiceDraftCreatedNotificationHandler
    : INotificationHandler<DomainEventNotification<WorkflowCompletedEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<InvoiceDraftCreatedNotificationHandler> _logger;

    public InvoiceDraftCreatedNotificationHandler(
        ISender sender,
        ILogger<InvoiceDraftCreatedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<WorkflowCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var workflowName = string.IsNullOrWhiteSpace(e.WorkflowName) ? "workflow" : $"'{e.WorkflowName}'";
        var invoiceRef   = $"WF-{e.WorkflowInstanceId.ToString("N")[..8].ToUpperInvariant()}";

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.StartedBy,
            Type:        NotificationType.InvoiceDraftCreated,
            Title:       "Invoice draft ready for review",
            Body:        $"A draft invoice ({invoiceRef}) has been automatically prepared from {workflowName}. Review and update it before sending.",
            CreatedBy:   e.StartedBy);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create InvoiceDraftCreated notification for workflow instance {Id}: {Error}",
                e.WorkflowInstanceId, result.Error);
    }
}
