using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

/// <summary>
/// Domain tests for <see cref="StepFieldDefinition"/> and the field-management
/// methods on <see cref="StepDefinition"/> / <see cref="WorkflowDefinition"/>.
/// All mutations are exercised through <c>WorkflowDefinition.AddStep(fields:…)</c>
/// to keep within the domain assembly's access rules.
/// </summary>
public sealed class StepFieldDefinitionDomainTests
{
    // ── AddField (via AddStep) ────────────────────────────────────────────────

    [Fact]
    public void AddStep_WithFields_AttachesFieldsInOrder()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        var fields = new[]
        {
            ("Hours Worked",  StepFieldType.Hours,   true,  (string?)null,        (IReadOnlyList<string>?)null),
            ("Notes",         StepFieldType.LongText, false, "Optional notes",     null),
        };

        // Act
        var step = definition.AddStep("Work Step", null, fields: fields);

        // Assert
        step.Fields.Should().HaveCount(2);
        step.Fields[0].Label.Should().Be("Hours Worked");
        step.Fields[0].FieldType.Should().Be(StepFieldType.Hours);
        step.Fields[0].IsRequired.Should().BeTrue();
        step.Fields[0].DisplayOrder.Should().Be(0);
        step.Fields[0].HelpText.Should().BeNull();

        step.Fields[1].Label.Should().Be("Notes");
        step.Fields[1].FieldType.Should().Be(StepFieldType.LongText);
        step.Fields[1].IsRequired.Should().BeFalse();
        step.Fields[1].DisplayOrder.Should().Be(1);
        step.Fields[1].HelpText.Should().Be("Optional notes");
    }

    [Fact]
    public void AddStep_WithDropdownField_HasCorrectOptions()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        IReadOnlyList<string> opts = ["Pending", "In Review", "Approved"];
        var fields = new[]
        {
            ("Status", StepFieldType.Dropdown, true, (string?)null, (IReadOnlyList<string>?)opts),
        };

        // Act
        var step = definition.AddStep("Review Step", null, fields: fields);

        // Assert
        step.Fields.Should().HaveCount(1);
        step.Fields[0].DropdownOptions.Should().BeEquivalentTo(["Pending", "In Review", "Approved"]);
    }

    [Fact]
    public void AddField_WithDropdownFewerThanTwoOptions_ThrowsDomainException()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        IReadOnlyList<string> oneOption = ["Only"];
        var fields = new[]
        {
            ("Status", StepFieldType.Dropdown, true, (string?)null, (IReadOnlyList<string>?)oneOption),
        };

        // Act
        var act = () => definition.AddStep("Bad Step", null, fields: fields);

        // Assert
        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*at least 2 options*");
    }

    [Fact]
    public void AddField_WithEmptyLabel_ThrowsDomainException()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        var fields = new[]
        {
            ("   ", StepFieldType.Text, false, (string?)null, (IReadOnlyList<string>?)null),
        };

        // Act
        var act = () => definition.AddStep("Step", null, fields: fields);

        // Assert
        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*label*");
    }

    // ── RemoveField ───────────────────────────────────────────────────────────

    [Fact]
    public void RemoveField_RenumbersRemainingFields()
    {
        // Arrange — step with 3 fields; remove the middle one
        var definition = new WorkflowDefinitionBuilder().Build();
        var fields = new[]
        {
            ("Field A", StepFieldType.Text,   false, (string?)null, (IReadOnlyList<string>?)null),
            ("Field B", StepFieldType.Number, false, null,          null),
            ("Field C", StepFieldType.Date,   false, null,          null),
        };
        var step = definition.AddStep("Triple Step", null, fields: fields);
        var middleId = step.Fields[1].Id;

        // Act — route through WorkflowDefinition to stay in the same assembly
        // (RemoveField is internal on StepDefinition; access via the aggregate in tests)
        step.RemoveField(middleId);

        // Assert
        step.Fields.Should().HaveCount(2);
        step.Fields[0].Label.Should().Be("Field A");
        step.Fields[0].DisplayOrder.Should().Be(0);
        step.Fields[1].Label.Should().Be("Field C");
        step.Fields[1].DisplayOrder.Should().Be(1);
    }

    [Fact]
    public void RemoveField_WithUnknownId_ThrowsDomainException()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        var step = definition.AddStep("Step", null);

        // Act
        var act = () => step.RemoveField(Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*not found*");
    }

    // ── MoveFieldUp / MoveFieldDown ───────────────────────────────────────────

    [Fact]
    public void MoveFieldUp_SwapsWithPreviousAndUpdatesDisplayOrders()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        var fields = new[]
        {
            ("Alpha", StepFieldType.Text, false, (string?)null, (IReadOnlyList<string>?)null),
            ("Beta",  StepFieldType.Text, false, null,          null),
        };
        var step = definition.AddStep("Step", null, fields: fields);
        var betaId = step.Fields[1].Id;

        // Act
        step.MoveFieldUp(betaId);

        // Assert
        step.Fields[0].Label.Should().Be("Beta");
        step.Fields[0].DisplayOrder.Should().Be(0);
        step.Fields[1].Label.Should().Be("Alpha");
        step.Fields[1].DisplayOrder.Should().Be(1);
    }

    [Fact]
    public void MoveFieldUp_WhenAlreadyFirst_DoesNothing()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        var fields = new[]
        {
            ("Alpha", StepFieldType.Text, false, (string?)null, (IReadOnlyList<string>?)null),
            ("Beta",  StepFieldType.Text, false, null,          null),
        };
        var step = definition.AddStep("Step", null, fields: fields);
        var alphaId = step.Fields[0].Id;

        // Act
        step.MoveFieldUp(alphaId);

        // Assert — unchanged
        step.Fields[0].Label.Should().Be("Alpha");
        step.Fields[1].Label.Should().Be("Beta");
    }

    [Fact]
    public void MoveFieldDown_SwapsWithNextAndUpdatesDisplayOrders()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        var fields = new[]
        {
            ("Alpha", StepFieldType.Text, false, (string?)null, (IReadOnlyList<string>?)null),
            ("Beta",  StepFieldType.Text, false, null,          null),
        };
        var step = definition.AddStep("Step", null, fields: fields);
        var alphaId = step.Fields[0].Id;

        // Act
        step.MoveFieldDown(alphaId);

        // Assert
        step.Fields[0].Label.Should().Be("Beta");
        step.Fields[0].DisplayOrder.Should().Be(0);
        step.Fields[1].Label.Should().Be("Alpha");
        step.Fields[1].DisplayOrder.Should().Be(1);
    }

    [Fact]
    public void MoveFieldDown_WhenAlreadyLast_DoesNothing()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        var fields = new[]
        {
            ("Alpha", StepFieldType.Text, false, (string?)null, (IReadOnlyList<string>?)null),
            ("Beta",  StepFieldType.Text, false, null,          null),
        };
        var step = definition.AddStep("Step", null, fields: fields);
        var betaId = step.Fields[1].Id;

        // Act
        step.MoveFieldDown(betaId);

        // Assert — unchanged
        step.Fields[0].Label.Should().Be("Alpha");
        step.Fields[1].Label.Should().Be("Beta");
    }

    // ── DropdownOptions round-trip ────────────────────────────────────────────

    [Fact]
    public void DropdownOptions_RoundTripsJsonCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        IReadOnlyList<string> opts = ["Red", "Green", "Blue"];
        var fields = new[]
        {
            ("Colour", StepFieldType.Dropdown, true, (string?)null, (IReadOnlyList<string>?)opts),
        };
        var step = definition.AddStep("Colour Step", null, fields: fields);

        // Assert — deserialized via the computed property
        step.Fields[0].DropdownOptions.Should().BeEquivalentTo(["Red", "Green", "Blue"],
            because: "DropdownOptionsJson must deserialize back to the original list");
    }

    [Fact]
    public void DropdownOptions_ForNonDropdownField_ReturnsEmptyList()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();
        var fields = new[]
        {
            ("Count", StepFieldType.Number, false, (string?)null, (IReadOnlyList<string>?)null),
        };
        var step = definition.AddStep("Count Step", null, fields: fields);

        // Assert
        step.Fields[0].DropdownOptions.Should().BeEmpty();
        step.Fields[0].DropdownOptionsJson.Should().BeNull();
    }
}
