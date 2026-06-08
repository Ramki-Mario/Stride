using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Queries.ExportStepFieldValuesCsv;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class ExportStepFieldValuesCsvQueryHandlerTests
{
    private readonly IWorkflowInstanceRepository   _instances   = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IWorkflowDefinitionRepository _definitions = Substitute.For<IWorkflowDefinitionRepository>();

    private ExportStepFieldValuesCsvQueryHandler Sut() =>
        new(_instances, _definitions, NullLogger<ExportStepFieldValuesCsvQueryHandler>.Instance);

    // ── Instance not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceNotFound_ReturnsFailure()
    {
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var result = await Sut().Handle(
            new ExportStepFieldValuesCsvQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── No field values → header-only CSV ────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStepHasNoFieldValues_ReturnsCsvWithHeaderOnly()
    {
        var definition = BuildDefinition("Hours Worked", StepFieldType.Hours);
        var instance   = BuildCompletedInstance(definition, fieldValues: null);

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _definitions.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await Sut().Handle(
            new ExportStepFieldValuesCsvQuery(instance.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var lines = result.Value!.Trim().Split('\n');
        lines.Should().HaveCount(1);
        lines[0].Should().Contain("StepOrder")
                         .And.Contain("StepName")
                         .And.Contain("FieldLabel");
    }

    // ── Field values present → rows in CSV ───────────────────────────────────

    [Fact]
    public async Task Handle_WhenFieldValuesExist_ReturnsCsvDataRows()
    {
        var definition = BuildDefinition("Hours Worked", StepFieldType.Hours);
        var fieldDefId = definition.Steps.First().Fields.Single().Id;
        var instance   = BuildCompletedInstance(definition, fieldValues: new[] { (fieldDefId, "3.5") });

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _definitions.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await Sut().Handle(
            new ExportStepFieldValuesCsvQuery(instance.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var csv = result.Value!;
        csv.Should().Contain("Step 1");        // StepName
        csv.Should().Contain("Hours Worked");  // FieldLabel from definition
        csv.Should().Contain("Hours");         // FieldType
        csv.Should().Contain("3.5");           // Value
    }

    // ── Label falls back to GUID when definition unavailable ─────────────────

    [Fact]
    public async Task Handle_WhenDefinitionUnavailable_UsesFieldDefGuidAsLabel()
    {
        var definition = BuildDefinition("Hours Worked", StepFieldType.Hours);
        var fieldDefId = definition.Steps.First().Fields.Single().Id;
        var instance   = BuildCompletedInstance(definition, fieldValues: new[] { (fieldDefId, "3.5") });

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _definitions.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowDefinition?)null);

        var result = await Sut().Handle(
            new ExportStepFieldValuesCsvQuery(instance.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().Contain(fieldDefId.ToString("N"));
    }

    // ── Multiple steps each with field values, ordered by step ───────────────

    [Fact]
    public async Task Handle_WithMultipleStepsAndValues_RowsOrderedByStep()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Multi-step", null, Guid.NewGuid());
        var s1  = def.AddStep("Intake", null);
        s1.AddField("Client Name", StepFieldType.Text, isRequired: true);
        var s2  = def.AddStep("Sign-off", null);
        s2.AddField("Approved Hours", StepFieldType.Hours, isRequired: false);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var f1Id = s1.Fields.Single().Id;
        var f2Id = s2.Fields.Single().Id;

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();

        // Complete step 1 then step 2
        var step1 = instance.Steps.OrderBy(s => s.Order).First();
        instance.CompleteStep(step1.Id, Guid.NewGuid(), fieldValues: new[] { (f1Id, "Acme Corp") });
        instance.ClearDomainEvents();

        var step2 = instance.Steps.OrderBy(s => s.Order).Last();
        instance.CompleteStep(step2.Id, Guid.NewGuid(), fieldValues: new[] { (f2Id, "8.0") });
        instance.ClearDomainEvents();

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _definitions.GetByIdAsync(def.Id, Arg.Any<CancellationToken>()).Returns(def);

        var result = await Sut().Handle(
            new ExportStepFieldValuesCsvQuery(instance.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var rows = result.Value!.Trim().Split('\n').Skip(1).Select(l => l.Trim()).ToList();
        rows.Should().HaveCount(2);
        rows[0].Should().Contain("Intake").And.Contain("Client Name").And.Contain("Acme Corp");
        rows[1].Should().Contain("Sign-off").And.Contain("Approved Hours").And.Contain("8.0");
    }

    // ── Values with commas / quotes are properly escaped ─────────────────────

    [Fact]
    public async Task Handle_WhenValueContainsSpecialChars_EscapedCorrectly()
    {
        var definition = BuildDefinition("Notes", StepFieldType.LongText);
        var fieldDefId = definition.Steps.First().Fields.Single().Id;
        var instance   = BuildCompletedInstance(definition,
            fieldValues: new[] { (fieldDefId, "He said \"hello\", world") });

        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        _definitions.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await Sut().Handle(
            new ExportStepFieldValuesCsvQuery(instance.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().Contain("\"He said \"\"hello\"\", world\"");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowDefinition BuildDefinition(string label, StepFieldType type)
    {
        var def  = WorkflowDefinition.Create(Guid.NewGuid(), "Test Workflow", null, Guid.NewGuid());
        var step = def.AddStep("Step 1", null);
        step.AddField(label, type, isRequired: false);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();
        return def;
    }

    private static WorkflowInstance BuildCompletedInstance(
        WorkflowDefinition definition,
        IReadOnlyList<(Guid, string)>? fieldValues)
    {
        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();
        var step = instance.Steps.OrderBy(s => s.Order).First();
        instance.CompleteStep(step.Id, Guid.NewGuid(), fieldValues: fieldValues);
        instance.ClearDomainEvents();
        return instance;
    }
}
