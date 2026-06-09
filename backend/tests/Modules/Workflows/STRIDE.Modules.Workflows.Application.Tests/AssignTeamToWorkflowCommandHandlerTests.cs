using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.AssignTeamToWorkflow;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class AssignTeamToWorkflowCommandHandlerTests
{
    private readonly IWorkflowInstanceRepository _instances   = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IAuditLogger                _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser                _currentUser = Substitute.For<ICurrentUser>();

    private readonly AssignTeamToWorkflowCommandHandler _sut;

    public AssignTeamToWorkflowCommandHandlerTests()
    {
        _currentUser.Email.Returns("actor@example.com");

        _sut = new AssignTeamToWorkflowCommandHandler(
            _instances,
            _audit,
            _currentUser,
            NullLogger<AssignTeamToWorkflowCommandHandler>.Instance);
    }

    // ── Success path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceExists_ReturnsSuccess()
    {
        // Arrange
        var instance = BuildRunningInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var cmd = new AssignTeamToWorkflowCommand(
            instance.Id, Guid.NewGuid(), Guid.NewGuid(), instance.TenantId);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenInstanceExists_SetsTeamIdAndSaves()
    {
        // Arrange
        var teamId   = Guid.NewGuid();
        var instance = BuildRunningInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var cmd = new AssignTeamToWorkflowCommand(
            instance.Id, teamId, Guid.NewGuid(), instance.TenantId);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        instance.TeamId.Should().Be(teamId);
        _instances.Received(1).Update(instance);
        await _instances.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInstanceExists_FiresAuditLog()
    {
        // Arrange
        var instance = BuildRunningInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var cmd = new AssignTeamToWorkflowCommand(
            instance.Id, Guid.NewGuid(), Guid.NewGuid(), instance.TenantId);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await _audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e =>
            e.Action       == AuditActions.WorkflowTeamAssigned &&
            e.ResourceType == "WorkflowInstance" &&
            e.ResourceId   == instance.Id));
    }

    [Fact]
    public async Task Handle_WithNullTeamId_ClearsAssignmentAndSaves()
    {
        // Arrange
        var instance = BuildRunningInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        // Assign a team first, then clear
        var cmd = new AssignTeamToWorkflowCommand(
            instance.Id, null, Guid.NewGuid(), instance.TenantId);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        instance.TeamId.Should().BeNull();
        _instances.Received(1).Update(instance);
    }

    // ── Failure path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceNotFound_ReturnsFailure()
    {
        // Arrange
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var cmd = new AssignTeamToWorkflowCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        _instances.DidNotReceive().Update(Arg.Any<WorkflowInstance>());
        await _instances.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static WorkflowInstance BuildRunningInstance()
    {
        var def = WorkflowDefinition.Create(
            Guid.NewGuid(), "Test Workflow", null, Guid.NewGuid());
        def.AddStep("Step 1", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
