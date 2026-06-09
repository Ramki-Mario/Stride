using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class WorkflowInstanceTests
{
    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_WithActiveDefinition_ReturnsRunningInstance()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.Active();
        var startedBy  = Guid.NewGuid();

        // Act
        var instance = WorkflowInstance.Start(definition, startedBy);

        // Assert
        instance.Id.Should().NotBeEmpty();
        instance.Status.Should().Be(WorkflowStatus.Running);
        instance.WorkflowDefinitionId.Should().Be(definition.Id);
        instance.StartedBy.Should().Be(startedBy);
        instance.TenantId.Should().Be(definition.TenantId);
        instance.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Start_WithActiveDefinition_SnapshotsStepsFromDefinition()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().WithSteps(3).Build();
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();

        // Act
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());

        // Assert
        instance.Steps.Should().HaveCount(3);
        instance.Steps.Select(s => s.Status)
            .Should().AllSatisfy(s => s.Should().Be(StepStatus.Pending));
    }

    [Fact]
    public void Start_WithDraftDefinition_ThrowsWorkflowDomainException()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.DraftWithOneStep();

        // Act
        var act = () => WorkflowInstance.Start(definition, Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public void Start_RaisesWorkflowStartedEvent()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.Active();
        var startedBy  = Guid.NewGuid();

        // Act
        var instance = WorkflowInstance.Start(definition, startedBy);

        // Assert
        instance.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkflowStartedEvent>();
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    [Fact]
    public void Pause_WhenRunning_SetsToPaused()
    {
        // Arrange
        var instance = BuildRunningInstance();

        // Act
        instance.Pause(Guid.NewGuid());

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Paused);
    }

    [Fact]
    public void Pause_WhenNotRunning_ThrowsWorkflowDomainException()
    {
        // Arrange
        var instance = BuildRunningInstance();
        instance.Pause(Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act — already paused, cannot pause again
        var act = () => instance.Pause(Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Running*");
    }

    [Fact]
    public void Resume_WhenPaused_SetsToRunning()
    {
        // Arrange
        var instance = BuildRunningInstance();
        instance.Pause(Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act
        instance.Resume(Guid.NewGuid());

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    [Fact]
    public void Resume_WhenNotPaused_ThrowsWorkflowDomainException()
    {
        // Arrange
        var instance = BuildRunningInstance();

        // Act — running, not paused
        var act = () => instance.Resume(Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Paused*");
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenRunning_SetsToCancelled()
    {
        // Arrange
        var instance = BuildRunningInstance();

        // Act
        instance.Cancel(Guid.NewGuid());

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenPaused_SetsToCancelled()
    {
        // Arrange
        var instance = BuildRunningInstance();
        instance.Pause(Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act
        instance.Cancel(Guid.NewGuid());

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsWorkflowDomainException()
    {
        // Arrange — complete all steps so instance auto-completes
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(1);
        instance.CompleteStep(stepIds[0], Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act
        var act = () => instance.Cancel(Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Running*Paused*");
    }

    // ── CompleteStep + auto-completion ────────────────────────────────────────

    [Fact]
    public void CompleteStep_LastRemainingStep_SetsInstanceToCompleted()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(1);

        // Act
        instance.CompleteStep(stepIds[0], Guid.NewGuid());

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Completed);
        instance.CompletedAt.Should().NotBeNull();
        instance.DomainEvents.Should().Contain(e => e is WorkflowCompletedEvent);
    }

    [Fact]
    public void CompleteStep_WithMoreStepsRemaining_KeepsRunningStatus()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act
        instance.CompleteStep(stepIds[0], Guid.NewGuid());

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    // ── FailStep ──────────────────────────────────────────────────────────────

    [Fact]
    public void FailStep_RequiredStep_SetsInstanceToFailed()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act
        instance.FailStep(stepIds[0], "Service unavailable", Guid.NewGuid());

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Failed);
        instance.DomainEvents.Should().Contain(e => e is WorkflowFailedEvent);
    }

    // ── SkipStep ──────────────────────────────────────────────────────────────

    [Fact]
    public void SkipStep_OptionalStep_Succeeds()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        definition.AddStep("Required Step", null, isRequired: true);
        definition.AddStep("Optional Step", null, isRequired: false);
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();

        var instance    = WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();

        var optionalStepId = instance.Steps.First(s => !s.IsRequired).Id;

        // Act
        instance.SkipStep(optionalStepId, Guid.NewGuid());

        // Assert
        instance.Steps.First(s => s.Id == optionalStepId)
            .Status.Should().Be(StepStatus.Skipped);
    }

    [Fact]
    public void SkipStep_RequiredStep_ThrowsWorkflowDomainException()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(1); // required by default

        // Act
        var act = () => instance.SkipStep(stepIds[0], Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Required*cannot be skipped*");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance BuildRunningInstance(int steps = 2)
    {
        var definition = new WorkflowDefinitionBuilder().WithSteps(steps).Build();
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }

    private static (WorkflowInstance instance, List<Guid> stepIds) BuildRunningInstanceWithStepIds(int steps)
    {
        var instance = BuildRunningInstance(steps);
        var stepIds  = instance.Steps.Select(s => s.Id).ToList();
        return (instance, stepIds);
    }
}
