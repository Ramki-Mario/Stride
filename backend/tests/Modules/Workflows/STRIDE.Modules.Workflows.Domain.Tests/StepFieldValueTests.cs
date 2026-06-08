using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class StepFieldValueTests
{
    [Fact]
    public void CompleteStep_WithFieldValues_AttachesValuesToStep()
    {
        var (instance, stepId, fieldId) = BuildRunningInstanceWithField();

        instance.CompleteStep(
            stepId,
            Guid.NewGuid(),
            fieldValues: new[]
            {
                (fieldId, "8.5"),
            });

        var step = instance.Steps.Single(s => s.Id == stepId);
        step.FieldValues.Should().HaveCount(1);
        step.FieldValues[0].StepInstanceId.Should().Be(stepId);
        step.FieldValues[0].StepFieldDefinitionId.Should().Be(fieldId);
        step.FieldValues[0].Value.Should().Be("8.5");
        step.FieldValues[0].TenantId.Should().Be(instance.TenantId);
    }

    [Fact]
    public void CompleteStep_WithMultipleFieldValues_AttachesAllValues()
    {
        var definition = BuildDefinitionWithFields(
            ("Hours worked", StepFieldType.Hours, true, null, null),
            ("Approved", StepFieldType.Boolean, false, null, null));
        var stepDefinition = definition.Steps[0];
        var fieldIds = stepDefinition.Fields.Select(f => f.Id).ToList();
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();
        var stepId = instance.Steps[0].Id;

        instance.CompleteStep(
            stepId,
            Guid.NewGuid(),
            fieldValues: new[]
            {
                (fieldIds[0], "4.25"),
                (fieldIds[1], "true"),
            });

        var values = instance.Steps[0].FieldValues;
        values.Should().HaveCount(2);
        values.Select(v => v.StepFieldDefinitionId).Should().BeEquivalentTo(fieldIds);
    }

    [Fact]
    public void CompleteStep_WithDuplicateFieldValue_ThrowsWorkflowDomainException()
    {
        var (instance, stepId, fieldId) = BuildRunningInstanceWithField();

        var act = () => instance.CompleteStep(
            stepId,
            Guid.NewGuid(),
            fieldValues: new[]
            {
                (fieldId, "first"),
                (fieldId, "second"),
            });

        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*submitted more than once*");
    }

    [Fact]
    public void CompleteStep_WithEmptyFieldDefinitionId_ThrowsWorkflowDomainException()
    {
        var (instance, stepId, _) = BuildRunningInstanceWithField();

        var act = () => instance.CompleteStep(
            stepId,
            Guid.NewGuid(),
            fieldValues: new[]
            {
                (Guid.Empty, "value"),
            });

        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*field definition id is required*");
    }

    private static (WorkflowInstance instance, Guid stepId, Guid fieldId) BuildRunningInstanceWithField()
    {
        var definition = BuildDefinitionWithFields(
            ("Hours worked", StepFieldType.Hours, true, null, null));
        var fieldId = definition.Steps[0].Fields[0].Id;
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();
        return (instance, instance.Steps[0].Id, fieldId);
    }

    private static WorkflowDefinition BuildDefinitionWithFields(
        params (string Label, StepFieldType FieldType, bool IsRequired, string? HelpText, IReadOnlyList<string>? DropdownOptions)[] fields)
    {
        var definition = WorkflowDefinition.Create(
            Guid.NewGuid(),
            "Runtime Field Workflow",
            null,
            Guid.NewGuid());

        definition.AddStep("Capture work", null, fields: fields);
        definition.Activate(Guid.NewGuid());
        definition.ClearDomainEvents();
        return definition;
    }
}
