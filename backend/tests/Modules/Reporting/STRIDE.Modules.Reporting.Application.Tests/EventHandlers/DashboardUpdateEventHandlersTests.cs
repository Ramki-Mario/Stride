using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Invoicing.Domain.Events;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.EventHandlers;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Reporting.Application.Tests.EventHandlers;

public sealed class DashboardUpdateEventHandlersTests
{
    private readonly IDashboardNotifier _notifier = Substitute.For<IDashboardNotifier>();

    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task StepCompleted_PublishesStepCompletedUpdateForTenant()
    {
        // Arrange
        var sut = new StepCompletedDashboardHandler(_notifier);
        var e   = new StepCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), TenantId, Guid.NewGuid(), "Review");

        // Act
        await sut.Handle(new DomainEventNotification<StepCompletedEvent>(e), CancellationToken.None);

        // Assert
        await _notifier.Received(1)
            .PublishAsync(TenantId, DashboardUpdates.StepCompleted, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StepAssigned_PublishesStepAssignedUpdateForTenant()
    {
        // Arrange
        var sut = new StepAssignedDashboardHandler(_notifier);
        var e   = new StepAssignedEvent(Guid.NewGuid(), Guid.NewGuid(), TenantId, Guid.NewGuid(), Guid.NewGuid(), "Review");

        // Act
        await sut.Handle(new DomainEventNotification<StepAssignedEvent>(e), CancellationToken.None);

        // Assert
        await _notifier.Received(1)
            .PublishAsync(TenantId, DashboardUpdates.StepAssigned, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StepOverdue_PublishesStepOverdueUpdateForTenant()
    {
        // Arrange
        var sut = new StepOverdueDashboardHandler(_notifier);
        var e   = new StepOverdueEvent(Guid.NewGuid(), Guid.NewGuid(), TenantId, Guid.NewGuid(), Guid.NewGuid(), "Review");

        // Act
        await sut.Handle(new DomainEventNotification<StepOverdueEvent>(e), CancellationToken.None);

        // Assert
        await _notifier.Received(1)
            .PublishAsync(TenantId, DashboardUpdates.StepOverdue, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WorkflowCompleted_PublishesWorkflowCompletedUpdateForTenant()
    {
        // Arrange
        var sut = new WorkflowCompletedDashboardHandler(_notifier);
        var e   = new WorkflowCompletedEvent(Guid.NewGuid(), TenantId, Guid.NewGuid(), "Job Alpha");

        // Act
        await sut.Handle(new DomainEventNotification<WorkflowCompletedEvent>(e), CancellationToken.None);

        // Assert
        await _notifier.Received(1)
            .PublishAsync(TenantId, DashboardUpdates.WorkflowCompleted, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvoiceGenerated_PublishesInvoiceCreatedUpdateForTenant()
    {
        // Arrange
        var sut = new InvoiceGeneratedDashboardHandler(_notifier);
        var e   = new InvoiceGeneratedEvent(Guid.NewGuid(), TenantId, "INV-0001", Guid.NewGuid());

        // Act
        await sut.Handle(new DomainEventNotification<InvoiceGeneratedEvent>(e), CancellationToken.None);

        // Assert
        await _notifier.Received(1)
            .PublishAsync(TenantId, DashboardUpdates.InvoiceCreated, Arg.Any<CancellationToken>());
    }
}
