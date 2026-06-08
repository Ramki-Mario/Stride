using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class OverdueTrackingTests
{
    // ── MarkStepOverdue ───────────────────────────────────────────────────────

    [Fact]
    public void MarkStepOverdue_SetsIsOverdueFlagOnStep()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var def = WorkflowDefinition.Create(tenantId, "Overdue Test", null, Guid.NewGuid());
        def.AddStep("Step 1", null, dueOffsetHours: 8m);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();
        var instance = WorkflowInstance_Start(def);
        var step = instance.Steps.First();

        // Act
        instance.MarkStepOverdue(step.Id);

        // Assert
        step.IsOverdue.Should().BeTrue();
        step.OverdueNotifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkStepOverdue_RaisesStepOverdueEvent()
    {
        // Arrange
        var def = WorkflowDefinitionBuilder.Active();
        var instance = WorkflowInstance_Start(def);
        var step = instance.Steps.First();

        // Act
        instance.MarkStepOverdue(step.Id);

        // Assert
        instance.DomainEvents.Should().ContainSingle(e => e is StepOverdueEvent);
        var ev = (StepOverdueEvent)instance.DomainEvents.Single(e => e is StepOverdueEvent);
        ev.StepInstanceId.Should().Be(step.Id);
        ev.WorkflowInstanceId.Should().Be(instance.Id);
        ev.TenantId.Should().Be(instance.TenantId);
        ev.StartedBy.Should().Be(instance.StartedBy);
    }

    [Fact]
    public void MarkStepOverdue_PopulatesAssigneeIdInEventWhenAssigned()
    {
        // Arrange
        var def = WorkflowDefinitionBuilder.Active();
        var instance = WorkflowInstance_Start(def);
        var step = instance.Steps.First();
        var assigneeId = Guid.NewGuid();
        instance.AssignStep(step.Id, assigneeId, Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act
        instance.MarkStepOverdue(step.Id);

        // Assert
        var ev = (StepOverdueEvent)instance.DomainEvents.Single(e => e is StepOverdueEvent);
        ev.AssigneeId.Should().Be(assigneeId);
    }

    [Fact]
    public void MarkStepOverdue_IsIdempotent_DoesNotRaiseDuplicateEvent()
    {
        // Arrange
        var def = WorkflowDefinitionBuilder.Active();
        var instance = WorkflowInstance_Start(def);
        var step = instance.Steps.First();
        instance.MarkStepOverdue(step.Id);
        instance.ClearDomainEvents();

        // Act — call again
        instance.MarkStepOverdue(step.Id);

        // Assert — no new event raised
        instance.DomainEvents.Should().BeEmpty();
        step.IsOverdue.Should().BeTrue(); // unchanged
    }

    [Fact]
    public void MarkStepOverdue_OnCompletedStep_IsNoOp()
    {
        // Arrange
        var def = WorkflowDefinitionBuilder.Active();
        var instance = WorkflowInstance_Start(def);
        var step = instance.Steps.First();
        instance.CompleteStep(step.Id, Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act
        instance.MarkStepOverdue(step.Id);

        // Assert — terminal step should not be flagged
        step.IsOverdue.Should().BeFalse();
        instance.DomainEvents.Should().BeEmpty();
    }

    // ── MarkSlaBreached ───────────────────────────────────────────────────────

    [Fact]
    public void MarkSlaBreached_SetsIsSlaBreachedAndNotifiedAt()
    {
        // Arrange
        var instance = SlaInstance(slaHours: 24m);

        // Act
        instance.MarkSlaBreached();

        // Assert
        instance.IsSlaBreached.Should().BeTrue();
        instance.SlaBreachedNotifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkSlaBreached_RaisesWorkflowSlaBreachedEvent()
    {
        // Arrange
        var instance = SlaInstance(slaHours: 24m);

        // Act
        instance.MarkSlaBreached();

        // Assert
        instance.DomainEvents.Should().ContainSingle(e => e is WorkflowSlaBreachedEvent);
        var ev = (WorkflowSlaBreachedEvent)instance.DomainEvents.Single(e => e is WorkflowSlaBreachedEvent);
        ev.WorkflowInstanceId.Should().Be(instance.Id);
        ev.TenantId.Should().Be(instance.TenantId);
        ev.StartedBy.Should().Be(instance.StartedBy);
        ev.DeadlineAt.Should().Be(instance.DeadlineAt!.Value);
    }

    [Fact]
    public void MarkSlaBreached_IsIdempotent_DoesNotRaiseDuplicateEvent()
    {
        // Arrange
        var instance = SlaInstance(slaHours: 24m);
        instance.MarkSlaBreached();
        instance.ClearDomainEvents();

        // Act — call again
        instance.MarkSlaBreached();

        // Assert
        instance.DomainEvents.Should().BeEmpty();
        instance.IsSlaBreached.Should().BeTrue(); // unchanged
    }

    [Fact]
    public void MarkSlaBreached_WithNoDeadline_IsNoOp()
    {
        // Arrange — no SLA configured
        var def = WorkflowDefinitionBuilder.Active();
        var instance = WorkflowInstance_Start(def);
        instance.ClearDomainEvents();

        // Act
        instance.MarkSlaBreached();

        // Assert
        instance.IsSlaBreached.Should().BeFalse();
        instance.DomainEvents.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance WorkflowInstance_Start(WorkflowDefinition def)
    {
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }

    private static WorkflowInstance SlaInstance(decimal slaHours)
    {
        var def = WorkflowDefinition.Create(
            Guid.NewGuid(), "SLA Workflow", null, Guid.NewGuid(), slaOffsetHours: slaHours);
        def.AddStep("Step 1", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
