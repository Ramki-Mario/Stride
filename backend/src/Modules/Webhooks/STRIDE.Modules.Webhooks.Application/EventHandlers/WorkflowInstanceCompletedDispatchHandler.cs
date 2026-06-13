using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Webhooks.Application.EventHandlers;

internal sealed class WorkflowInstanceCompletedDispatchHandler
    : INotificationHandler<DomainEventNotification<WorkflowCompletedEvent>>
{
    private readonly IWebhookDispatcher                                _dispatcher;
    private readonly ILogger<WorkflowInstanceCompletedDispatchHandler> _logger;

    public WorkflowInstanceCompletedDispatchHandler(
        IWebhookDispatcher dispatcher,
        ILogger<WorkflowInstanceCompletedDispatchHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Handle(
        DomainEventNotification<WorkflowCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        try
        {
            await _dispatcher.DispatchAsync(
                tenantId:  e.TenantId,
                eventType: Domain.WebhookEventTypes.WorkflowInstanceCompleted,
                data: new
                {
                    workflowInstanceId = e.WorkflowInstanceId,
                    workflowName       = e.WorkflowName,
                    startedBy          = e.StartedBy,
                    clientId           = e.ClientId,
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception dispatching webhook for workflow.instance.completed (instance {Id})",
                e.WorkflowInstanceId);
        }
    }
}
