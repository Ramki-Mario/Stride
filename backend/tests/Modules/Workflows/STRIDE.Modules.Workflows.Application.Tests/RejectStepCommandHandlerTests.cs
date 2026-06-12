using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.RejectStep;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class RejectStepCommandHandlerTests
{
    private readonly IWorkflowInstanceRepository _instances = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IUserRoleService            _roles     = Substitute.For<IUserRoleService>();
    private readonly RejectStepCommandHandler    _sut;

    public RejectStepCommandHandlerTests()
    {
        _sut = new RejectStepCommandHandler(_instances, _roles, NullLogger<RejectStepCommandHandler>.Instance);
    }

    // ── Instance not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceNotFound_ReturnsFailure()
    {
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(
            new RejectStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Needs more detail"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── HaltWorkflow handling ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithHaltHandling_RejectsStepAndHaltsWorkflow()
    {
        var instance = BuildInstanceWithApprovalGate(RejectionHandling.HaltWorkflow);
        var step     = instance.Steps.Single();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new RejectStepCommand(instance.Id, step.Id, Guid.NewGuid(), "Quality is not acceptable"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(StepStatus.Rejected);
        instance.Status.Should().Be(WorkflowStatus.Halted);
        instance.ApprovalRequests.Single().Comment.Should().Be("Quality is not acceptable");
        _instances.Received(1).Update(instance);
        await _instances.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── RevertToStep handling ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithRevertHandling_RevertsTargetAndKeepsWorkflowRunning()
    {
        var instance = BuildTwoStepInstanceWithRevertGate();
        var standard = instance.Steps.First(s => s.Order == 0);
        var gate     = instance.Steps.First(s => s.Order == 1);

        // Complete the standard step to activate the gate.
        instance.CompleteStep(standard.Id, Guid.NewGuid());
        instance.ClearDomainEvents();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new RejectStepCommand(instance.Id, gate.Id, Guid.NewGuid(), "Please redo the work"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        gate.Status.Should().Be(StepStatus.Rejected);
        standard.Status.Should().Be(StepStatus.Pending);
        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    // ── RequiredRoleId — user does NOT hold role ──────────────────────────────

    [Fact]
    public async Task Handle_WhenUserLacksRequiredRole_ReturnsFailureWithoutSaving()
    {
        var roleId   = Guid.NewGuid();
        var instance = BuildInstanceWithApprovalGate(RejectionHandling.HaltWorkflow, requiredRoleId: roleId);
        var step     = instance.Steps.Single();
        var userId   = Guid.NewGuid();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _roles.UserHasRoleAsync(userId, roleId, instance.TenantId, Arg.Any<CancellationToken>())
              .Returns(false);

        var result = await _sut.Handle(
            new RejectStepCommand(instance.Id, step.Id, userId, "Not authorised anyway"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required role");
        instance.Status.Should().Be(WorkflowStatus.Running);
        await _instances.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Domain guard — step is not an approval gate ───────────────────────────

    [Fact]
    public async Task Handle_WhenStepIsNotAwaitingApproval_ReturnsFailureFromDomainGuard()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Standard WF", null, Guid.NewGuid());
        def.AddStep("Step A", null);
        def.Activate(Guid.NewGuid());
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        var step = instance.Steps.Single();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new RejectStepCommand(instance.Id, step.Id, Guid.NewGuid(), "Cannot reject this step"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _instances.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance BuildInstanceWithApprovalGate(
        RejectionHandling rejectionHandling,
        Guid? requiredRoleId = null)
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Approval WF", null, Guid.NewGuid());
        def.AddStep("Gate", null, requiredRoleId: requiredRoleId,
            stepType: StepType.Approval, rejectionHandling: rejectionHandling);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }

    private static WorkflowInstance BuildTwoStepInstanceWithRevertGate()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Revert WF", null, Guid.NewGuid());
        def.AddStep("Work", null);
        def.AddStep("Gate", null, stepType: StepType.Approval,
            rejectionHandling: RejectionHandling.RevertToStep, revertToStepOrder: 0);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
