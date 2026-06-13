using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Webhooks.Application.EventHandlers;

internal sealed class WorkflowStepOverdueDispatchHandler
    : INotificationHandler<DomainEventNotification<StepOverdueEvent>>
{
    private readonly IWebhookDispatcher                            _dispatcher;
    private readonly ILogger<WorkflowStepOverdueDispatchHandler>   _logger;

    public WorkflowStepOverdueDispatchHandler(
        IWebhookDispatcher dispatcher,
        ILogger<WorkflowStepOverdueDispatchHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Handle(
        DomainEventNotification<StepOverdueEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        try
        {
            await _dispatcher.DispatchAsync(
                tenantId:  e.TenantId,
                eventType: Domain.WebhookEventTypes.WorkflowStepOverdue,
                data: new
                {
                    stepInstanceId     = e.StepInstanceId,
                    workflowInstanceId = e.WorkflowInstanceId,
                    stepName           = e.StepName,
                    assigneeId         = e.AssigneeId,
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception dispatching webhook for workflow.step.overdue (step {Id})",
                e.StepInstanceId);
        }
    }
}
