using System.Text.Json;
using MediatR;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Workflows.Application.EventHandlers;

/// <summary>
/// Writes a <see cref="WorkflowActivityEvent"/> row for each step-lifecycle domain event.
/// </summary>
internal sealed class StepAssignedActivityHandler
    : INotificationHandler<DomainEventNotification<StepAssignedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public StepAssignedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<StepAssignedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var payload = StepPayloadHelper.BuildPayload(stepName: e.StepName);

        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.StepAssigned,
            e.AssignedBy,
            payload);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class StepCompletedActivityHandler
    : INotificationHandler<DomainEventNotification<StepCompletedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public StepCompletedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<StepCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var payload = StepPayloadHelper.BuildPayload(stepName: e.StepName);

        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.StepCompleted,
            e.CompletedBy,
            payload);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class StepFailedActivityHandler
    : INotificationHandler<DomainEventNotification<StepFailedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public StepFailedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<StepFailedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var payload = StepPayloadHelper.BuildPayload(stepName: e.StepName, reason: e.Reason);

        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.StepFailed,
            e.FailedBy,
            payload);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class StepSkippedActivityHandler
    : INotificationHandler<DomainEventNotification<StepSkippedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public StepSkippedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<StepSkippedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var payload = StepPayloadHelper.BuildPayload(stepName: e.StepName);

        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.StepSkipped,
            e.SkippedBy,
            payload);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class StepOverdueActivityHandler
    : INotificationHandler<DomainEventNotification<StepOverdueEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public StepOverdueActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<StepOverdueEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var payload = StepPayloadHelper.BuildPayload(stepName: e.StepName);

        // Use StartedBy as the actor (system/background job context; no explicit actor user).
        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.StepOverdue,
            e.StartedBy,
            payload);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

// ── Shared helpers ─────────────────────────────────────────────────────────

file static class StepPayloadHelper
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    internal static string? BuildPayload(string? stepName = null, string? reason = null)
    {
        var dict = new Dictionary<string, string?>();

        if (!string.IsNullOrEmpty(stepName)) dict["stepName"] = stepName;
        if (!string.IsNullOrEmpty(reason))   dict["reason"]   = reason;

        return dict.Count > 0
            ? JsonSerializer.Serialize(dict, Options)
            : null;
    }
}
