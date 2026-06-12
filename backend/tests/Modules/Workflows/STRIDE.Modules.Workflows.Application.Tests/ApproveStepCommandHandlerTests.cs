using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.ApproveStep;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class ApproveStepCommandHandlerTests
{
    private readonly IWorkflowInstanceRepository _instances = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IUserRoleService            _roles     = Substitute.For<IUserRoleService>();
    private readonly ApproveStepCommandHandler   _sut;

    public ApproveStepCommandHandlerTests()
    {
        _sut = new ApproveStepCommandHandler(_instances, _roles, NullLogger<ApproveStepCommandHandler>.Instance);
    }

    // ── Instance not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceNotFound_ReturnsFailure()
    {
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(
            new ApproveStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Step not found ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStepNotFound_ReturnsFailure()
    {
        var instance = BuildInstanceWithApprovalGate();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new ApproveStepCommand(instance.Id, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── No RequiredRoleId — anyone can approve ────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoRequiredRole_ApprovesStepAndSaves()
    {
        var instance = BuildInstanceWithApprovalGate(requiredRoleId: null);
        var step     = instance.Steps.Single();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new ApproveStepCommand(instance.Id, step.Id, Guid.NewGuid(), "Looks good to me"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(StepStatus.Completed);
        _instances.Received(1).Update(instance);
        await _instances.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _roles.DidNotReceive().UserHasRoleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── RequiredRoleId — user holds role ──────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserHoldsRequiredRole_Succeeds()
    {
        var roleId   = Guid.NewGuid();
        var instance = BuildInstanceWithApprovalGate(requiredRoleId: roleId);
        var step     = instance.Steps.Single();
        var userId   = Guid.NewGuid();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _roles.UserHasRoleAsync(userId, roleId, instance.TenantId, Arg.Any<CancellationToken>())
              .Returns(true);

        var result = await _sut.Handle(
            new ApproveStepCommand(instance.Id, step.Id, userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(StepStatus.Completed);
    }

    // ── RequiredRoleId — user does NOT hold role ──────────────────────────────

    [Fact]
    public async Task Handle_WhenUserLacksRequiredRole_ReturnsFailureWithoutSaving()
    {
        var roleId   = Guid.NewGuid();
        var instance = BuildInstanceWithApprovalGate(requiredRoleId: roleId);
        var step     = instance.Steps.Single();
        var userId   = Guid.NewGuid();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _roles.UserHasRoleAsync(userId, roleId, instance.TenantId, Arg.Any<CancellationToken>())
              .Returns(false);

        var result = await _sut.Handle(
            new ApproveStepCommand(instance.Id, step.Id, userId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required role");
        step.Status.Should().Be(StepStatus.AwaitingApproval);
        await _instances.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Domain guard — step is not an approval gate ───────────────────────────

    [Fact]
    public async Task Handle_WhenStepIsNotAwaitingApproval_ReturnsFailureFromDomainGuard()
    {
        var instance = BuildInstanceWithStandardStep();
        var step     = instance.Steps.Single();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new ApproveStepCommand(instance.Id, step.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _instances.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance BuildInstanceWithApprovalGate(Guid? requiredRoleId = null)
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Approval WF", null, Guid.NewGuid());
        def.AddStep("Gate", null, requiredRoleId: requiredRoleId, stepType: StepType.Approval);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        // First step is an approval gate — Start auto-activates it to AwaitingApproval.
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }

    private static WorkflowInstance BuildInstanceWithStandardStep()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Standard WF", null, Guid.NewGuid());
        def.AddStep("Step A", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
