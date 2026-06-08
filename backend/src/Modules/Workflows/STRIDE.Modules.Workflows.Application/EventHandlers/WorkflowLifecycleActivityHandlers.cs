using MediatR;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Workflows.Application.EventHandlers;

/// <summary>
/// Writes a <see cref="WorkflowActivityEvent"/> row for each workflow-lifecycle domain event.
/// </summary>
internal sealed class WorkflowStartedActivityHandler
    : INotificationHandler<DomainEventNotification<WorkflowStartedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public WorkflowStartedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<WorkflowStartedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.WorkflowStarted,
            e.StartedBy);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class WorkflowCompletedActivityHandler
    : INotificationHandler<DomainEventNotification<WorkflowCompletedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public WorkflowCompletedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<WorkflowCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.WorkflowCompleted,
            e.StartedBy);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class WorkflowCancelledActivityHandler
    : INotificationHandler<DomainEventNotification<WorkflowCancelledEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public WorkflowCancelledActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<WorkflowCancelledEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.WorkflowCancelled,
            e.CancelledBy);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class WorkflowSlaBreachedActivityHandler
    : INotificationHandler<DomainEventNotification<WorkflowSlaBreachedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public WorkflowSlaBreachedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<WorkflowSlaBreachedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.WorkflowSlaBreached,
            e.StartedBy,
            occurredAt: e.DeadlineAt);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class WorkflowPausedActivityHandler
    : INotificationHandler<DomainEventNotification<WorkflowPausedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public WorkflowPausedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<WorkflowPausedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.WorkflowPaused,
            e.PausedBy);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class WorkflowResumedActivityHandler
    : INotificationHandler<DomainEventNotification<WorkflowResumedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public WorkflowResumedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<WorkflowResumedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.WorkflowResumed,
            e.ResumedBy);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}
