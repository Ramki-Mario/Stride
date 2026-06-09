using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class ApprovalGateTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance StartWithApprovalStep(
        RejectionHandling rejectionHandling = RejectionHandling.HaltWorkflow,
        int? revertToStepOrder = null)
    {
        var tenantId = Guid.NewGuid();
        var def = WorkflowDefinition.Create(tenantId, "Approval WF", null, Guid.NewGuid());
        def.AddStep("Step 1", null); // Standard step first
        def.AddStep("Approve Me", null, stepType: StepType.Approval,
            rejectionHandling: rejectionHandling, revertToStepOrder: revertToStepOrder);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }

    private static WorkflowInstance StartWithApprovalStepFirst(
        RejectionHandling rejectionHandling = RejectionHandling.HaltWorkflow)
    {
        var tenantId = Guid.NewGuid();
        var def = WorkflowDefinition.Create(tenantId, "Approval WF First", null, Guid.NewGuid());
        def.AddStep("Gate First", null, stepType: StepType.Approval, rejectionHandling: rejectionHandling);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }

    // ── Auto-activation ───────────────────────────────────────────────────────

    [Fact]
    public void Start_WhenFirstStepIsApproval_AutoActivatesItToAwaitingApproval()
    {
        var instance = StartWithApprovalStepFirst();

        instance.Steps.Single().Status.Should().Be(StepStatus.AwaitingApproval);
        instance.ApprovalRequests.Should().HaveCount(1);
        instance.ApprovalRequests.Single().Status.Should().Be(ApprovalStatus.Pending);
    }

    [Fact]
    public void CompleteStep_WhenNextStepIsApproval_AutoActivatesApprovalStep()
    {
        var instance = StartWithApprovalStep();
        var standardStep = instance.Steps.First(s => s.StepType == StepType.Standard);

        instance.CompleteStep(standardStep.Id, Guid.NewGuid());

        var approvalStep = instance.Steps.First(s => s.StepType == StepType.Approval);
        approvalStep.Status.Should().Be(StepStatus.AwaitingApproval);
        instance.ApprovalRequests.Should().HaveCount(1);
    }

    [Fact]
    public void SkipStep_WhenNextStepIsApproval_AutoActivatesApprovalStep()
    {
        var tenantId = Guid.NewGuid();
        var def = WorkflowDefinition.Create(tenantId, "WF", null, Guid.NewGuid());
        def.AddStep("Optional", null, isRequired: false); // skippable
        def.AddStep("Gate", null, stepType: StepType.Approval);
        def.Activate(Guid.NewGuid());
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();

        var optionalStep = instance.Steps.First(s => s.StepType == StepType.Standard);
        instance.SkipStep(optionalStep.Id, Guid.NewGuid());

        var approvalStep = instance.Steps.First(s => s.StepType == StepType.Approval);
        approvalStep.Status.Should().Be(StepStatus.AwaitingApproval);
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public void ApproveStep_OnAwaitingApprovalStep_TransitionsToCompleted()
    {
        var instance = StartWithApprovalStepFirst();
        var approvalStep = instance.Steps.Single();
        var approver = Guid.NewGuid();

        instance.ApproveStep(approvalStep.Id, approver, "Looks good");

        approvalStep.Status.Should().Be(StepStatus.Completed);
        instance.ApprovalRequests.Single().Status.Should().Be(ApprovalStatus.Approved);
        instance.ApprovalRequests.Single().DecisionByUserId.Should().Be(approver);
        instance.ApprovalRequests.Single().Comment.Should().Be("Looks good");
    }

    [Fact]
    public void ApproveStep_LastStep_CompletesWorkflow()
    {
        var instance = StartWithApprovalStepFirst();
        var approvalStep = instance.Steps.Single();

        instance.ApproveStep(approvalStep.Id, Guid.NewGuid());

        instance.Status.Should().Be(WorkflowStatus.Completed);
        instance.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void ApproveStep_RaisesStepApprovedEvent()
    {
        var instance = StartWithApprovalStepFirst();
        var step = instance.Steps.Single();

        instance.ApproveStep(step.Id, Guid.NewGuid());

        instance.DomainEvents.Should().ContainSingle(e => e is StepApprovedEvent);
    }

    [Fact]
    public void ApproveStep_OnNonApprovalAwaitingStep_ThrowsWorkflowDomainException()
    {
        var instance = StartWithApprovalStep();
        var standardStep = instance.Steps.First(s => s.StepType == StepType.Standard);

        var act = () => instance.ApproveStep(standardStep.Id, Guid.NewGuid());

        act.Should().Throw<WorkflowDomainException>();
    }

    // ── Reject — HaltWorkflow ─────────────────────────────────────────────────

    [Fact]
    public void RejectStep_WithHaltWorkflow_TransitionsToRejectedAndHaltsWorkflow()
    {
        var instance = StartWithApprovalStepFirst(RejectionHandling.HaltWorkflow);
        var step = instance.Steps.Single();

        instance.RejectStep(step.Id, Guid.NewGuid(), "Needs revision");

        step.Status.Should().Be(StepStatus.Rejected);
        instance.Status.Should().Be(WorkflowStatus.Halted);
        instance.ApprovalRequests.Single().Status.Should().Be(ApprovalStatus.Rejected);
        instance.ApprovalRequests.Single().Comment.Should().Be("Needs revision");
    }

    [Fact]
    public void RejectStep_WithHaltWorkflow_RaisesWorkflowHaltedEvent()
    {
        var instance = StartWithApprovalStepFirst(RejectionHandling.HaltWorkflow);

        instance.RejectStep(instance.Steps.Single().Id, Guid.NewGuid());

        instance.DomainEvents.Should().ContainSingle(e => e is WorkflowHaltedEvent);
    }

    // ── Reject — RevertToStep ─────────────────────────────────────────────────

    [Fact]
    public void RejectStep_WithRevertToStep_ResetsTargetStepToPending()
    {
        // Arrange: Standard (order 0) → Approval (order 1, reverts to 0)
        var instance = StartWithApprovalStep(
            rejectionHandling: RejectionHandling.RevertToStep,
            revertToStepOrder: 0);

        var standardStep  = instance.Steps.First(s => s.Order == 0);
        var approvalStep  = instance.Steps.First(s => s.Order == 1);

        // Complete standard step to activate the approval gate
        instance.CompleteStep(standardStep.Id, Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act: reject the approval
        instance.RejectStep(approvalStep.Id, Guid.NewGuid());

        // Assert
        approvalStep.Status.Should().Be(StepStatus.Rejected);
        standardStep.Status.Should().Be(StepStatus.Pending);
        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    [Fact]
    public void RejectStep_WithRevertToStep_DoesNotHaltWorkflow()
    {
        var instance = StartWithApprovalStep(
            rejectionHandling: RejectionHandling.RevertToStep,
            revertToStepOrder: 0);

        var standardStep = instance.Steps.First(s => s.Order == 0);
        instance.CompleteStep(standardStep.Id, Guid.NewGuid());
        instance.ClearDomainEvents();

        instance.RejectStep(instance.Steps.First(s => s.Order == 1).Id, Guid.NewGuid());

        instance.DomainEvents.Should().NotContain(e => e is WorkflowHaltedEvent);
        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    // ── Guard conditions ──────────────────────────────────────────────────────

    [Fact]
    public void ApproveStep_OnHaltedWorkflow_ThrowsWorkflowDomainException()
    {
        var instance = StartWithApprovalStepFirst();
        var step = instance.Steps.Single();
        instance.RejectStep(step.Id, Guid.NewGuid()); // Halts the workflow

        // A new approval request must be created or re-activated; just assert guard fires
        var act = () => instance.ApproveStep(step.Id, Guid.NewGuid());
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Running*");
    }

    [Fact]
    public void ApproveStep_WhenNoPendingApprovalRequest_ThrowsWorkflowDomainException()
    {
        var instance = StartWithApprovalStepFirst();
        var step = instance.Steps.Single();
        instance.ApproveStep(step.Id, Guid.NewGuid()); // Approve once — request is now Approved

        // Attempt a second approval on the same (now Completed) step
        var act = () => instance.ApproveStep(step.Id, Guid.NewGuid());
        act.Should().Throw<WorkflowDomainException>();
    }

    // ── StepType validation ───────────────────────────────────────────────────

    [Fact]
    public void StepDefinition_Create_WithRevertToStepHandling_RequiresRevertOrder()
    {
        var act = () => StepDefinition.Create(
            workflowDefinitionId: Guid.NewGuid(),
            name: "Gate",
            description: null,
            order: 1,
            stepType: StepType.Approval,
            rejectionHandling: RejectionHandling.RevertToStep,
            revertToStepOrder: null); // missing!

        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*RevertToStepOrder*");
    }
}
