using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.ClaimStep;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class ClaimStepCommandHandlerTests
{
    private readonly IWorkflowInstanceRepository _instances = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IUserRoleService            _roles     = Substitute.For<IUserRoleService>();
    private readonly ClaimStepCommandHandler     _sut;

    public ClaimStepCommandHandlerTests()
    {
        _sut = new ClaimStepCommandHandler(_instances, _roles, NullLogger<ClaimStepCommandHandler>.Instance);
    }

    // ── Instance not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceNotFound_ReturnsFailure()
    {
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(
            new ClaimStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Step not found ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStepNotFound_ReturnsFailure()
    {
        var instance = BuildRunningInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new ClaimStepCommand(instance.Id, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── No RequiredRoleId — anyone can claim ──────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoRequiredRole_ClaimSucceeds()
    {
        var instance = BuildRunningInstance(requiredRoleId: null);
        var stepId   = instance.Steps[0].Id;
        var userId   = Guid.NewGuid();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new ClaimStepCommand(instance.Id, stepId, userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _roles.DidNotReceive().UserHasRoleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── RequiredRoleId — user holds role ──────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserHoldsRequiredRole_ClaimSucceeds()
    {
        var roleId   = Guid.NewGuid();
        var instance = BuildRunningInstance(requiredRoleId: roleId);
        var stepId   = instance.Steps[0].Id;
        var userId   = Guid.NewGuid();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _roles.UserHasRoleAsync(userId, roleId, instance.TenantId, Arg.Any<CancellationToken>())
              .Returns(true);

        var result = await _sut.Handle(
            new ClaimStepCommand(instance.Id, stepId, userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── RequiredRoleId — user does NOT hold role ──────────────────────────────

    [Fact]
    public async Task Handle_WhenUserLacksRequiredRole_ReturnsFailure()
    {
        var roleId   = Guid.NewGuid();
        var instance = BuildRunningInstance(requiredRoleId: roleId);
        var stepId   = instance.Steps[0].Id;
        var userId   = Guid.NewGuid();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _roles.UserHasRoleAsync(userId, roleId, instance.TenantId, Arg.Any<CancellationToken>())
              .Returns(false);

        var result = await _sut.Handle(
            new ClaimStepCommand(instance.Id, stepId, userId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required role");
    }

    // ── Successful claim saves changes ────────────────────────────────────────

    [Fact]
    public async Task Handle_OnSuccess_SavesChanges()
    {
        var instance = BuildRunningInstance(requiredRoleId: null);
        var stepId   = instance.Steps[0].Id;

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        await _sut.Handle(
            new ClaimStepCommand(instance.Id, stepId, Guid.NewGuid()),
            CancellationToken.None);

        _instances.Received(1).Update(instance);
        await _instances.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
