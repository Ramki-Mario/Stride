using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Webhooks.Application.EventHandlers;

internal sealed class WorkflowStepCompletedDispatchHandler
    : INotificationHandler<DomainEventNotification<StepCompletedEvent>>
{
    private readonly IWebhookDispatcher                              _dispatcher;
    private readonly ILogger<WorkflowStepCompletedDispatchHandler>   _logger;

    public WorkflowStepCompletedDispatchHandler(
        IWebhookDispatcher dispatcher,
        ILogger<WorkflowStepCompletedDispatchHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Handle(
        DomainEventNotification<StepCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        try
        {
            await _dispatcher.DispatchAsync(
                tenantId:  e.TenantId,
                eventType: Domain.WebhookEventTypes.WorkflowStepCompleted,
                data: new
                {
                    stepInstanceId     = e.StepInstanceId,
                    workflowInstanceId = e.WorkflowInstanceId,
                    stepName           = e.StepName,
                    completedBy        = e.CompletedBy,
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception dispatching webhook for workflow.step.completed (step {Id})",
                e.StepInstanceId);
        }
    }
}
