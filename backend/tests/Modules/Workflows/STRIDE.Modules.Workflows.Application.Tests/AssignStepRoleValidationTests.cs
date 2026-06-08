using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.AssignStep;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// Verifies the role-validation extension added to AssignStepCommandHandler in US-148.
/// </summary>
public sealed class AssignStepRoleValidationTests
{
    private readonly IWorkflowInstanceRepository _instances = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IUserRoleService            _roles     = Substitute.For<IUserRoleService>();
    private readonly AssignStepCommandHandler    _sut;

    public AssignStepRoleValidationTests()
    {
        _sut = new AssignStepCommandHandler(_instances, _roles, NullLogger<AssignStepCommandHandler>.Instance);
    }

    // ── No RequiredRoleId — role service not called ───────────────────────────

    [Fact]
    public async Task Handle_WhenStepHasNoRequiredRole_SkipsRoleCheck()
    {
        var instance = BuildRunningInstance(requiredRoleId: null);
        var step     = instance.Steps[0];

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new AssignStepCommand(instance.Id, step.Id, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _roles.DidNotReceive().UserHasRoleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Assignee holds required role — allowed ────────────────────────────────

    [Fact]
    public async Task Handle_WhenAssigneeHoldsRequiredRole_Succeeds()
    {
        var roleId   = Guid.NewGuid();
        var instance = BuildRunningInstance(requiredRoleId: roleId);
        var step     = instance.Steps[0];
        var assignee = Guid.NewGuid();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _roles.UserHasRoleAsync(assignee, roleId, instance.TenantId, Arg.Any<CancellationToken>())
              .Returns(true);

        var result = await _sut.Handle(
            new AssignStepCommand(instance.Id, step.Id, assignee, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Assignee does not hold required role — rejected ───────────────────────

    [Fact]
    public async Task Handle_WhenAssigneeLacksRequiredRole_ReturnsFailure()
    {
        var roleId   = Guid.NewGuid();
        var instance = BuildRunningInstance(requiredRoleId: roleId);
        var step     = instance.Steps[0];
        var assignee = Guid.NewGuid();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _roles.UserHasRoleAsync(assignee, roleId, instance.TenantId, Arg.Any<CancellationToken>())
              .Returns(false);

        var result = await _sut.Handle(
            new AssignStepCommand(instance.Id, step.Id, assignee, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required role");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance BuildRunningInstance(Guid? requiredRoleId = null)
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Test WF", null, Guid.NewGuid());
        def.AddStep("Step A", null, requiredRoleId: requiredRoleId);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
