using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Tests;

/// <summary>
/// US-173 domain coverage: multi-step revert (intermediate steps marked Reverted,
/// StepsRevertedEvent raised) and ResumeFromHalt / Cancel transitions on Halted workflows.
/// Complements <see cref="ApprovalGateTests"/> which covers the US-171 surface.
/// </summary>
public sealed class ApprovalGateRevertAndResumeTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Step A (0, standard) → Step B (1, standard) → Gate (2, approval, reverts to 0).</summary>
    private static WorkflowInstance StartThreeStepRevertWorkflow()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Revert WF", null, Guid.NewGuid());
        def.AddStep("Step A", null);
        def.AddStep("Step B", null);
        def.AddStep("Gate", null, stepType: StepType.Approval,
            rejectionHandling: RejectionHandling.RevertToStep, revertToStepOrder: 0);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }

    private static WorkflowInstance StartHaltedWorkflow()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Halt WF", null, Guid.NewGuid());
        def.AddStep("Gate", null, stepType: StepType.Approval,
            rejectionHandling: RejectionHandling.HaltWorkflow);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.RejectStep(instance.Steps.Single().Id, Guid.NewGuid(), "Halting for test");
        instance.ClearDomainEvents();
        return instance;
    }

    // ── RevertToStep with intermediate steps ──────────────────────────────────

    [Fact]
    public void RejectStep_WithRevertToStep_MarksIntermediateCompletedStepsReverted()
    {
        var instance = StartThreeStepRevertWorkflow();
        var stepA = instance.Steps.First(s => s.Order == 0);
        var stepB = instance.Steps.First(s => s.Order == 1);
        var gate  = instance.Steps.First(s => s.Order == 2);

        instance.CompleteStep(stepA.Id, Guid.NewGuid());
        instance.CompleteStep(stepB.Id, Guid.NewGuid()); // auto-activates the gate
        instance.ClearDomainEvents();

        instance.RejectStep(gate.Id, Guid.NewGuid(), "Start over from Step A");

        gate.Status.Should().Be(StepStatus.Rejected);
        stepB.Status.Should().Be(StepStatus.Reverted);
        stepA.Status.Should().Be(StepStatus.Pending);
        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    [Fact]
    public void RejectStep_WithRevertToStep_RaisesStepsRevertedEventListingIntermediates()
    {
        var instance = StartThreeStepRevertWorkflow();
        var stepA = instance.Steps.First(s => s.Order == 0);
        var stepB = instance.Steps.First(s => s.Order == 1);
        var gate  = instance.Steps.First(s => s.Order == 2);
        var rejectedBy = Guid.NewGuid();

        instance.CompleteStep(stepA.Id, Guid.NewGuid());
        instance.CompleteStep(stepB.Id, Guid.NewGuid());
        instance.ClearDomainEvents();

        instance.RejectStep(gate.Id, rejectedBy, "Start over");

        var revertedEvent = instance.DomainEvents.OfType<StepsRevertedEvent>().Single();
        revertedEvent.RejectedStepInstanceId.Should().Be(gate.Id);
        revertedEvent.TargetStepOrder.Should().Be(0);
        revertedEvent.RejectedBy.Should().Be(rejectedBy);
        revertedEvent.Comment.Should().Be("Start over");
        revertedEvent.RevertedSteps.Should().ContainSingle(s => s.StepInstanceId == stepB.Id);
    }

    [Fact]
    public void RejectStep_WithAdjacentRevertTarget_DoesNotRaiseStepsRevertedEvent()
    {
        // Standard (0) → Gate (1, reverts to 0): no intermediate steps exist.
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Adjacent WF", null, Guid.NewGuid());
        def.AddStep("Work", null);
        def.AddStep("Gate", null, stepType: StepType.Approval,
            rejectionHandling: RejectionHandling.RevertToStep, revertToStepOrder: 0);
        def.Activate(Guid.NewGuid());
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());

        var work = instance.Steps.First(s => s.Order == 0);
        var gate = instance.Steps.First(s => s.Order == 1);
        instance.CompleteStep(work.Id, Guid.NewGuid());
        instance.ClearDomainEvents();

        instance.RejectStep(gate.Id, Guid.NewGuid(), "Redo it");

        instance.DomainEvents.Should().NotContain(e => e is StepsRevertedEvent);
        work.Status.Should().Be(StepStatus.Pending);
    }

    // ── ResumeFromHalt ────────────────────────────────────────────────────────

    [Fact]
    public void ResumeFromHalt_OnHaltedWorkflow_TransitionsToRunning()
    {
        var instance = StartHaltedWorkflow();

        instance.ResumeFromHalt(Guid.NewGuid());

        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    [Fact]
    public void ResumeFromHalt_RaisesWorkflowResumedEvent()
    {
        var instance = StartHaltedWorkflow();
        var resumedBy = Guid.NewGuid();

        instance.ResumeFromHalt(resumedBy);

        instance.DomainEvents.OfType<WorkflowResumedEvent>()
            .Should().ContainSingle(e => e.ResumedBy == resumedBy);
    }

    [Fact]
    public void ResumeFromHalt_WhenNotHalted_ThrowsWorkflowDomainException()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Running WF", null, Guid.NewGuid());
        def.AddStep("Step A", null);
        def.Activate(Guid.NewGuid());
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());

        var act = () => instance.ResumeFromHalt(Guid.NewGuid());

        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Halted*");
    }

    // ── Cancel on Halted ──────────────────────────────────────────────────────

    [Fact]
    public void Cancel_OnHaltedWorkflow_TransitionsToCancelled()
    {
        var instance = StartHaltedWorkflow();

        instance.Cancel(Guid.NewGuid());

        instance.Status.Should().Be(WorkflowStatus.Cancelled);
        instance.DomainEvents.Should().ContainSingle(e => e is WorkflowCancelledEvent);
    }
}
