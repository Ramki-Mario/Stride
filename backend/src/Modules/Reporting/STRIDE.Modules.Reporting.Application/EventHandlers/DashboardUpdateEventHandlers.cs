using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Invoicing.Domain.Events;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Reporting.Application.EventHandlers;

/// <summary>
/// Bridges domain events to the real-time dashboard (US-176).
///
/// Each handler forwards a tenant-scoped update through <see cref="IDashboardNotifier"/>
/// so connected dashboard clients can refetch the affected panels. Handlers are
/// deliberately thin — the notifier swallows and logs publish failures, so a Redis
/// outage can never roll back the business operation that raised the event.
/// </summary>
internal sealed class StepCompletedDashboardHandler
    : INotificationHandler<DomainEventNotification<StepCompletedEvent>>
{
    private readonly IDashboardNotifier _notifier;
    public StepCompletedDashboardHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task Handle(DomainEventNotification<StepCompletedEvent> notification, CancellationToken cancellationToken) =>
        _notifier.PublishAsync(notification.DomainEvent.TenantId, DashboardUpdates.StepCompleted, cancellationToken);
}

internal sealed class StepAssignedDashboardHandler
    : INotificationHandler<DomainEventNotification<StepAssignedEvent>>
{
    private readonly IDashboardNotifier _notifier;
    public StepAssignedDashboardHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task Handle(DomainEventNotification<StepAssignedEvent> notification, CancellationToken cancellationToken) =>
        _notifier.PublishAsync(notification.DomainEvent.TenantId, DashboardUpdates.StepAssigned, cancellationToken);
}

internal sealed class StepOverdueDashboardHandler
    : INotificationHandler<DomainEventNotification<StepOverdueEvent>>
{
    private readonly IDashboardNotifier _notifier;
    public StepOverdueDashboardHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task Handle(DomainEventNotification<StepOverdueEvent> notification, CancellationToken cancellationToken) =>
        _notifier.PublishAsync(notification.DomainEvent.TenantId, DashboardUpdates.StepOverdue, cancellationToken);
}

internal sealed class WorkflowCompletedDashboardHandler
    : INotificationHandler<DomainEventNotification<WorkflowCompletedEvent>>
{
    private readonly IDashboardNotifier _notifier;
    public WorkflowCompletedDashboardHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task Handle(DomainEventNotification<WorkflowCompletedEvent> notification, CancellationToken cancellationToken) =>
        _notifier.PublishAsync(notification.DomainEvent.TenantId, DashboardUpdates.WorkflowCompleted, cancellationToken);
}

internal sealed class InvoiceGeneratedDashboardHandler
    : INotificationHandler<DomainEventNotification<InvoiceGeneratedEvent>>
{
    private readonly IDashboardNotifier _notifier;
    public InvoiceGeneratedDashboardHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task Handle(DomainEventNotification<InvoiceGeneratedEvent> notification, CancellationToken cancellationToken) =>
        _notifier.PublishAsync(notification.DomainEvent.TenantId, DashboardUpdates.InvoiceCreated, cancellationToken);
}
