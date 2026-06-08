using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class WorkflowSlaTests
{
    // ── WorkflowDefinition — SlaOffsetHours validation ────────────────────────

    [Fact]
    public void Create_WithPositiveSlaOffsetHours_SetsSlaOffsetHours()
    {
        // Act
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "SLA Workflow", null, Guid.NewGuid(), slaOffsetHours: 48m);

        // Assert
        definition.SlaOffsetHours.Should().Be(48m);
    }

    [Fact]
    public void Create_WithZeroSlaOffsetHours_ThrowsWorkflowDomainException()
    {
        // Act
        var act = () => WorkflowDefinition.Create(Guid.NewGuid(), "Bad SLA", null, Guid.NewGuid(), slaOffsetHours: 0m);

        // Assert
        act.Should().Throw<WorkflowDomainException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_WithNegativeSlaOffsetHours_ThrowsWorkflowDomainException()
    {
        // Act
        var act = () => WorkflowDefinition.Create(Guid.NewGuid(), "Bad SLA", null, Guid.NewGuid(), slaOffsetHours: -8m);

        // Assert
        act.Should().Throw<WorkflowDomainException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_WithNullSlaOffsetHours_LeavesPropertyNull()
    {
        // Act
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "No SLA", null, Guid.NewGuid());

        // Assert
        definition.SlaOffsetHours.Should().BeNull();
    }

    // ── WorkflowInstance — DeadlineAt calculated on Start ─────────────────────

    [Fact]
    public void Start_WhenDefinitionHasSlaOffsetHours_SetsDeadlineAt()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().WithSteps(1).Build();
        definition.GetType()
            .GetProperty(nameof(WorkflowDefinition.SlaOffsetHours))!
            .SetValue(definition, 24m);

        // Use a definition created via the public factory to test the full path
        var defWithSla = WorkflowDefinition.Create(Guid.NewGuid(), "SLA WF", null, Guid.NewGuid(), slaOffsetHours: 24m);
        defWithSla.AddStep("Only Step", null);
        defWithSla.Activate(Guid.NewGuid());
        defWithSla.ClearDomainEvents();

        // Act
        var before = DateTime.UtcNow;
        var instance = WorkflowInstance.Start(defWithSla, Guid.NewGuid());
        var after = DateTime.UtcNow;

        // Assert
        instance.DeadlineAt.Should().NotBeNull();
        instance.DeadlineAt!.Value.Should()
            .BeOnOrAfter(before.AddHours(24))
            .And
            .BeOnOrBefore(after.AddHours(24));
    }

    [Fact]
    public void Start_WhenDefinitionHasNoSla_DeadlineAtIsNull()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.Active();

        // Act
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());

        // Assert
        instance.DeadlineAt.Should().BeNull();
    }

    [Fact]
    public void Start_DeadlineAt_IsCreatedAtPlusSlaOffsetHours()
    {
        // Arrange
        var defWithSla = WorkflowDefinition.Create(Guid.NewGuid(), "SLA WF 2", null, Guid.NewGuid(), slaOffsetHours: 72m);
        defWithSla.AddStep("Step", null);
        defWithSla.Activate(Guid.NewGuid());
        defWithSla.ClearDomainEvents();

        // Act
        var instance = WorkflowInstance.Start(defWithSla, Guid.NewGuid());

        // Assert
        var expectedDeadline = instance.CreatedAt.AddHours(72);
        instance.DeadlineAt!.Value.Should().BeCloseTo(expectedDeadline, precision: TimeSpan.FromSeconds(2));
    }
}
