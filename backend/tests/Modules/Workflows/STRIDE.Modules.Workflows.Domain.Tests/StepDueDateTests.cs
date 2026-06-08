using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class StepDueDateTests
{
    // ── StepDefinition validation ─────────────────────────────────────────────

    [Fact]
    public void AddStep_WithPositiveDueOffsetHours_SetsDueOffsetHours()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();

        // Act
        var step = definition.AddStep("Review", null, dueOffsetHours: 24m);

        // Assert
        step.DueOffsetHours.Should().Be(24m);
    }

    [Fact]
    public void AddStep_WithZeroDueOffsetHours_ThrowsWorkflowDomainException()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();

        // Act
        var act = () => definition.AddStep("Review", null, dueOffsetHours: 0m);

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void AddStep_WithNegativeDueOffsetHours_ThrowsWorkflowDomainException()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();

        // Act
        var act = () => definition.AddStep("Review", null, dueOffsetHours: -1m);

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void AddStep_WithNullDueOffsetHours_LeavesDueOffsetNull()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();

        // Act
        var step = definition.AddStep("Review", null, dueOffsetHours: null);

        // Assert
        step.DueOffsetHours.Should().BeNull();
    }

    // ── Instance creation — step 1 DueAt calculated from CreatedAt ────────────

    [Fact]
    public void Start_WhenStep1HasDueOffsetHours_SetsDueAtOnFirstStepOnly()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        definition.AddStep("Step 1", null, dueOffsetHours: 48m);
        definition.AddStep("Step 2", null, dueOffsetHours: 24m);
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();

        // Act
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());

        // Assert
        var steps = instance.Steps.OrderBy(s => s.Order).ToList();
        steps[0].DueAt.Should().NotBeNull("step 1 should have DueAt calculated on Start");
        steps[0].DueAt!.Value.Should().BeCloseTo(
            instance.CreatedAt.AddHours(48), precision: TimeSpan.FromSeconds(5));

        steps[1].DueAt.Should().BeNull("subsequent steps DueAt is set when predecessor completes");
    }

    [Fact]
    public void Start_WhenStep1HasNoDueOffsetHours_DueAtIsNullOnAllSteps()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        definition.AddStep("Step 1", null);
        definition.AddStep("Step 2", null, dueOffsetHours: 24m);
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();

        // Act
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());

        // Assert
        instance.Steps.Should().AllSatisfy(s => s.DueAt.Should().BeNull());
    }

    // ── CompleteStep — subsequent step DueAt recalculated from completion time ─

    [Fact]
    public void CompleteStep_WhenNextStepHasDueOffsetHours_SetsDueAtFromCompletionTime()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        definition.AddStep("Step 1", null);
        definition.AddStep("Step 2", null, dueOffsetHours: 8m);
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();

        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();
        var step1 = instance.Steps.OrderBy(s => s.Order).First();

        // Act
        instance.CompleteStep(step1.Id, Guid.NewGuid());

        // Assert
        var step2 = instance.Steps.OrderBy(s => s.Order).Skip(1).First();
        step2.DueAt.Should().NotBeNull();
        step2.DueAt!.Value.Should().BeCloseTo(
            step1.CompletedAt!.Value.AddHours(8), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CompleteStep_WhenNextStepHasNoDueOffsetHours_LeavesDueAtNull()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        definition.AddStep("Step 1", null);
        definition.AddStep("Step 2", null);  // no due offset
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();

        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();
        var step1 = instance.Steps.OrderBy(s => s.Order).First();

        // Act
        instance.CompleteStep(step1.Id, Guid.NewGuid());

        // Assert
        var step2 = instance.Steps.OrderBy(s => s.Order).Skip(1).First();
        step2.DueAt.Should().BeNull();
    }

    // ── Snapshot — DueOffsetHours snapshotted from definition to instance ──────

    [Fact]
    public void StepInstance_SnapshotsDueOffsetHoursFromDefinition()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        definition.AddStep("Approval", null, dueOffsetHours: 72m);
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();

        // Act
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());

        // Assert
        instance.Steps.Single().DueOffsetHours.Should().Be(72m);
    }
}
