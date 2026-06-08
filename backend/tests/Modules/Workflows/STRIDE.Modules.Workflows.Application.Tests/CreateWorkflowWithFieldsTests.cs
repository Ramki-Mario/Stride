using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.CreateWorkflow;
using STRIDE.Modules.Workflows.Application.Queries.GetWorkflowDefinition;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// Verifies that field definitions are threaded correctly from the
/// <see cref="CreateWorkflowCommand"/> through the domain aggregate and back out
/// via the <see cref="GetWorkflowDefinitionQueryHandler"/> DTO projection.
/// </summary>
public sealed class CreateWorkflowWithFieldsTests
{
    private readonly IWorkflowDefinitionRepository _repo =
        Substitute.For<IWorkflowDefinitionRepository>();

    // ── CreateWorkflow persists field definitions ─────────────────────────────

    [Fact]
    public async Task CreateWorkflow_WithFieldsOnStep_FieldsStoredOnDefinition()
    {
        // Arrange
        var sut = new CreateWorkflowCommandHandler(
            _repo, NullLogger<CreateWorkflowCommandHandler>.Instance);

        WorkflowDefinition? saved = null;
        await _repo.AddAsync(
            Arg.Do<WorkflowDefinition>(d => saved = d),
            Arg.Any<CancellationToken>());

        var cmd = new CreateWorkflowCommand(
            TenantId:    Guid.NewGuid(),
            Name:        "Onboarding",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps: new[]
            {
                new StepRequest(
                    "Paperwork",
                    null,
                    IsRequired: true,
                    RequiredRoleId: null,
                    FieldDefinitions: new[]
                    {
                        new FieldDefinitionRequest("Hours Worked", StepFieldType.Hours,  true),
                        new FieldDefinitionRequest("Notes",        StepFieldType.LongText, false, "Any remarks"),
                    }),
            });

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.Steps.Should().HaveCount(1);
        saved.Steps[0].Fields.Should().HaveCount(2);

        saved.Steps[0].Fields[0].Label.Should().Be("Hours Worked");
        saved.Steps[0].Fields[0].FieldType.Should().Be(StepFieldType.Hours);
        saved.Steps[0].Fields[0].IsRequired.Should().BeTrue();
        saved.Steps[0].Fields[0].DisplayOrder.Should().Be(0);

        saved.Steps[0].Fields[1].Label.Should().Be("Notes");
        saved.Steps[0].Fields[1].FieldType.Should().Be(StepFieldType.LongText);
        saved.Steps[0].Fields[1].IsRequired.Should().BeFalse();
        saved.Steps[0].Fields[1].HelpText.Should().Be("Any remarks");
        saved.Steps[0].Fields[1].DisplayOrder.Should().Be(1);
    }

    [Fact]
    public async Task CreateWorkflow_WithDropdownField_DropdownOptionsStored()
    {
        // Arrange
        var sut = new CreateWorkflowCommandHandler(
            _repo, NullLogger<CreateWorkflowCommandHandler>.Instance);

        WorkflowDefinition? saved = null;
        await _repo.AddAsync(
            Arg.Do<WorkflowDefinition>(d => saved = d),
            Arg.Any<CancellationToken>());

        var cmd = new CreateWorkflowCommand(
            TenantId:    Guid.NewGuid(),
            Name:        "Status WF",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps: new[]
            {
                new StepRequest(
                    "Review",
                    null,
                    FieldDefinitions: new[]
                    {
                        new FieldDefinitionRequest(
                            "Outcome",
                            StepFieldType.Dropdown,
                            true,
                            DropdownOptions: new[] { "Approved", "Rejected", "Pending" }),
                    }),
            });

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        saved!.Steps[0].Fields[0].DropdownOptions
              .Should().BeEquivalentTo(["Approved", "Rejected", "Pending"]);
    }

    [Fact]
    public async Task CreateWorkflow_StepWithNoFields_FieldsCollectionIsEmpty()
    {
        // Arrange
        var sut = new CreateWorkflowCommandHandler(
            _repo, NullLogger<CreateWorkflowCommandHandler>.Instance);

        WorkflowDefinition? saved = null;
        await _repo.AddAsync(
            Arg.Do<WorkflowDefinition>(d => saved = d),
            Arg.Any<CancellationToken>());

        var cmd = new CreateWorkflowCommand(
            TenantId:    Guid.NewGuid(),
            Name:        "Simple WF",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps: new[] { new StepRequest("Step 1", null) });

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        saved!.Steps[0].Fields.Should().BeEmpty();
    }

    // ── GetWorkflowDefinition DTO projection ──────────────────────────────────

    [Fact]
    public async Task GetWorkflowDefinition_WhenStepHasFields_DtoContainsFieldsOrderedByDisplayOrder()
    {
        // Arrange
        var sut = new CreateWorkflowCommandHandler(
            _repo, NullLogger<CreateWorkflowCommandHandler>.Instance);

        WorkflowDefinition? saved = null;
        await _repo.AddAsync(
            Arg.Do<WorkflowDefinition>(d => saved = d),
            Arg.Any<CancellationToken>());

        var createCmd = new CreateWorkflowCommand(
            TenantId:    Guid.NewGuid(),
            Name:        "Field WF",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps: new[]
            {
                new StepRequest(
                    "Data Entry",
                    null,
                    FieldDefinitions: new[]
                    {
                        new FieldDefinitionRequest("Text Field",  StepFieldType.Text,   false),
                        new FieldDefinitionRequest("Num Field",   StepFieldType.Number, true),
                    }),
            });

        await sut.Handle(createCmd, CancellationToken.None);
        saved.Should().NotBeNull();

        _repo.GetByIdAsync(saved!.Id, Arg.Any<CancellationToken>())
             .Returns(saved);

        var getHandler = new GetWorkflowDefinitionQueryHandler(
            _repo,
            NullLogger<GetWorkflowDefinitionQueryHandler>.Instance);

        // Act
        var result = await getHandler.Handle(
            new GetWorkflowDefinitionQuery(saved.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Steps.Should().HaveCount(1);

        var fields = result.Value.Steps[0].Fields;
        fields.Should().HaveCount(2);

        fields[0].Label.Should().Be("Text Field");
        fields[0].FieldType.Should().Be("Text");
        fields[0].IsRequired.Should().BeFalse();
        fields[0].DisplayOrder.Should().Be(0);

        fields[1].Label.Should().Be("Num Field");
        fields[1].FieldType.Should().Be("Number");
        fields[1].IsRequired.Should().BeTrue();
        fields[1].DisplayOrder.Should().Be(1);
    }

    [Fact]
    public async Task GetWorkflowDefinition_DropdownField_DropdownOptionsInDto()
    {
        // Arrange
        var sut = new CreateWorkflowCommandHandler(
            _repo, NullLogger<CreateWorkflowCommandHandler>.Instance);

        WorkflowDefinition? saved = null;
        await _repo.AddAsync(
            Arg.Do<WorkflowDefinition>(d => saved = d),
            Arg.Any<CancellationToken>());

        var createCmd = new CreateWorkflowCommand(
            TenantId:    Guid.NewGuid(),
            Name:        "Dropdown WF",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps: new[]
            {
                new StepRequest(
                    "Pick",
                    null,
                    FieldDefinitions: new[]
                    {
                        new FieldDefinitionRequest(
                            "Priority",
                            StepFieldType.Dropdown,
                            true,
                            DropdownOptions: new[] { "Low", "Medium", "High" }),
                    }),
            });

        await sut.Handle(createCmd, CancellationToken.None);
        _repo.GetByIdAsync(saved!.Id, Arg.Any<CancellationToken>()).Returns(saved);

        var getHandler = new GetWorkflowDefinitionQueryHandler(
            _repo,
            NullLogger<GetWorkflowDefinitionQueryHandler>.Instance);

        // Act
        var result = await getHandler.Handle(
            new GetWorkflowDefinitionQuery(saved.Id), CancellationToken.None);

        // Assert
        result.Value!.Steps[0].Fields[0].DropdownOptions
              .Should().BeEquivalentTo(["Low", "Medium", "High"]);
    }
}
