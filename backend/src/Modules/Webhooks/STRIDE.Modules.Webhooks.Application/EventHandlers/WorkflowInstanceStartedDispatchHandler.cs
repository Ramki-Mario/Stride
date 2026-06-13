using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Webhooks.Application.EventHandlers;

/// <summary>
/// Fans out a <c>workflow.instance.started</c> webhook payload to all tenant
/// subscriptions that include this event type whenever a workflow instance starts.
/// </summary>
internal sealed class WorkflowInstanceStartedDispatchHandler
    : INotificationHandler<DomainEventNotification<WorkflowStartedEvent>>
{
    private readonly IWebhookDispatcher                               _dispatcher;
    private readonly ILogger<WorkflowInstanceStartedDispatchHandler>  _logger;

    public WorkflowInstanceStartedDispatchHandler(
        IWebhookDispatcher dispatcher,
        ILogger<WorkflowInstanceStartedDispatchHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Handle(
        DomainEventNotification<WorkflowStartedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        try
        {
            await _dispatcher.DispatchAsync(
                tenantId:  e.TenantId,
                eventType: Domain.WebhookEventTypes.WorkflowInstanceStarted,
                data: new
                {
                    workflowInstanceId = e.WorkflowInstanceId,
                    definitionId       = e.DefinitionId,
                    workflowName       = e.WorkflowName,
                    startedBy          = e.StartedBy,
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception dispatching webhook for workflow.instance.started (instance {Id})",
                e.WorkflowInstanceId);
        }
    }
}
