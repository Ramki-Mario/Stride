using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

/// <summary>
/// Tests for <see cref="BillableItem"/> validation rules (via the public
/// <see cref="WorkflowInstance.CompleteStep"/> aggregate-root API, which
/// internally calls <c>BillableItem.Create</c>).
/// </summary>
public sealed class BillableItemTests
{
    // ── Validation — BillableItem.Create ─────────────────────────────────────

    [Fact]
    public void CompleteStep_WithValidBillableItem_AttachesItemToStep()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act
        instance.CompleteStep(stepIds[0], Guid.NewGuid(), new[]
        {
            ("Labour", 3.0m, 75.00m, BillableUnit.Hours),
        });

        // Assert
        var completedStep = instance.Steps.Single(s => s.Id == stepIds[0]);
        completedStep.BillableItems.Should().HaveCount(1);
        completedStep.BillableItems[0].Description.Should().Be("Labour");
        completedStep.BillableItems[0].Quantity.Should().Be(3.0m);
        completedStep.BillableItems[0].UnitPrice.Should().Be(75.00m);
        completedStep.BillableItems[0].LineTotal.Should().Be(225.00m);
        completedStep.BillableItems[0].Unit.Should().Be(BillableUnit.Hours);
    }

    [Fact]
    public void CompleteStep_WithMultipleBillableItems_AttachesAllItems()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act
        instance.CompleteStep(stepIds[0], Guid.NewGuid(), new[]
        {
            ("Labour",          2.0m, 90.00m, BillableUnit.Hours),
            ("Replacement part", 1.0m, 45.50m, BillableUnit.Each),
            ("Call-out fee",     1.0m, 60.00m, BillableUnit.Fixed),
        });

        // Assert
        var step = instance.Steps.Single(s => s.Id == stepIds[0]);
        step.BillableItems.Should().HaveCount(3);
        step.BillableItems.Sum(b => b.LineTotal).Should().Be(2.0m * 90.00m + 45.50m + 60.00m);
    }

    [Fact]
    public void CompleteStep_WithNoBillableItems_StepHasEmptyBillableList()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act — billableItems is null
        instance.CompleteStep(stepIds[0], Guid.NewGuid());

        // Assert
        var step = instance.Steps.Single(s => s.Id == stepIds[0]);
        step.BillableItems.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CompleteStep_WithEmptyDescription_ThrowsWorkflowDomainException(string description)
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act
        var act = () => instance.CompleteStep(stepIds[0], Guid.NewGuid(), new[]
        {
            (description, 1.0m, 10.00m, BillableUnit.Each),
        });

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*description*cannot be empty*");
    }

    [Fact]
    public void CompleteStep_WithZeroQuantity_ThrowsWorkflowDomainException()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act
        var act = () => instance.CompleteStep(stepIds[0], Guid.NewGuid(), new[]
        {
            ("Labour", 0m, 10.00m, BillableUnit.Hours),
        });

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*quantity*greater than zero*");
    }

    [Fact]
    public void CompleteStep_WithNegativeQuantity_ThrowsWorkflowDomainException()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act
        var act = () => instance.CompleteStep(stepIds[0], Guid.NewGuid(), new[]
        {
            ("Labour", -1m, 10.00m, BillableUnit.Hours),
        });

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*quantity*greater than zero*");
    }

    [Fact]
    public void CompleteStep_WithZeroUnitPrice_ThrowsWorkflowDomainException()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);

        // Act
        var act = () => instance.CompleteStep(stepIds[0], Guid.NewGuid(), new[]
        {
            ("Labour", 1.0m, 0m, BillableUnit.Hours),
        });

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*unit price*greater than zero*");
    }

    [Fact]
    public void BillableItem_LineTotal_EqualsQuantityTimesUnitPrice()
    {
        // Arrange
        var (instance, stepIds) = BuildRunningInstanceWithStepIds(2);
        instance.CompleteStep(stepIds[0], Guid.NewGuid(), new[]
        {
            ("Testing", 4.5m, 12.25m, BillableUnit.Hours),
        });

        // Assert
        var item = instance.Steps.Single(s => s.Id == stepIds[0]).BillableItems[0];
        item.LineTotal.Should().Be(4.5m * 12.25m);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (STRIDE.Modules.Workflows.Domain.Entities.WorkflowInstance instance, List<Guid> stepIds)
        BuildRunningInstanceWithStepIds(int steps)
    {
        var definition = new WorkflowDefinitionBuilder().WithSteps(steps).Build();
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();
        var instance = STRIDE.Modules.Workflows.Domain.Entities.WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();
        var stepIds = instance.Steps.Select(s => s.Id).ToList();
        return (instance, stepIds);
    }
}
