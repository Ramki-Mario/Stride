using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class AssignTeamTests
{
    // ── AssignTeam ─────────────────────────────────────────────────────────────

    [Fact]
    public void AssignTeam_SetsTeamIdOnInstance()
    {
        // Arrange
        var instance = RunningInstance();
        var teamId   = Guid.NewGuid();

        // Act
        instance.AssignTeam(teamId, Guid.NewGuid());

        // Assert
        instance.TeamId.Should().Be(teamId);
    }

    [Fact]
    public void AssignTeam_RaisesWorkflowTeamAssignedEvent()
    {
        // Arrange
        var instance   = RunningInstance();
        var teamId     = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        // Act
        instance.AssignTeam(teamId, assignedBy);

        // Assert
        var evt = instance.DomainEvents
            .OfType<WorkflowTeamAssignedEvent>()
            .Single();

        evt.WorkflowInstanceId.Should().Be(instance.Id);
        evt.TeamId.Should().Be(teamId);
        evt.AssignedBy.Should().Be(assignedBy);
    }

    [Fact]
    public void AssignTeam_WithNullTeamId_ClearsAssignment()
    {
        // Arrange
        var instance = RunningInstance();
        instance.AssignTeam(Guid.NewGuid(), Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act
        instance.AssignTeam(null, Guid.NewGuid());

        // Assert
        instance.TeamId.Should().BeNull();
    }

    [Fact]
    public void AssignTeam_WithNullTeamId_RaisesEventWithNullTeamId()
    {
        // Arrange
        var instance = RunningInstance();
        instance.ClearDomainEvents();

        // Act
        instance.AssignTeam(null, Guid.NewGuid());

        // Assert
        var evt = instance.DomainEvents
            .OfType<WorkflowTeamAssignedEvent>()
            .Single();

        evt.TeamId.Should().BeNull();
    }

    [Fact]
    public void AssignTeam_UpdatesUpdatedAt()
    {
        // Arrange
        var instance = RunningInstance();
        var before   = instance.UpdatedAt;

        // Act
        instance.AssignTeam(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        instance.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void AssignTeam_CanBeCalledOnCompletedInstance()
    {
        // Arrange — complete the single step so the instance reaches Completed status
        var instance = RunningInstance();
        var step     = instance.Steps.First();
        instance.CompleteStep(step.Id, Guid.NewGuid());
        instance.ClearDomainEvents();

        // Act — assigning team to a completed instance should still work
        var act = () => instance.AssignTeam(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        act.Should().NotThrow();
        instance.TeamId.Should().NotBeNull();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static WorkflowInstance RunningInstance()
    {
        var def = WorkflowDefinitionBuilder.Active();
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
