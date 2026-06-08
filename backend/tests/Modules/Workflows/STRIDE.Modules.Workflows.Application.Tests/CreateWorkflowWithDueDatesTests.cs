using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.CreateWorkflow;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// Verifies that DueOffsetHours is threaded correctly from
/// CreateWorkflowCommand through the domain aggregate and persisted.
/// </summary>
public sealed class CreateWorkflowWithDueDatesTests
{
    private readonly IWorkflowDefinitionRepository _repo =
        Substitute.For<IWorkflowDefinitionRepository>();

    [Fact]
    public async Task CreateWorkflow_WithDueOffsetHoursOnStep_PersistedOnDefinition()
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
            Name:        "SLA Workflow",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps: new[]
            {
                new StepRequest("Initial Review",  null, DueOffsetHours: 24m),
                new StepRequest("Final Approval",  null, DueOffsetHours: 48m),
            });

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();

        var steps = saved!.Steps.OrderBy(s => s.Order).ToList();
        steps[0].DueOffsetHours.Should().Be(24m);
        steps[1].DueOffsetHours.Should().Be(48m);
    }

    [Fact]
    public async Task CreateWorkflow_WithZeroDueOffsetHours_ReturnsFailure()
    {
        // Arrange
        var sut = new CreateWorkflowCommandHandler(
            _repo, NullLogger<CreateWorkflowCommandHandler>.Instance);

        var cmd = new CreateWorkflowCommand(
            TenantId:    Guid.NewGuid(),
            Name:        "Bad Workflow",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps: new[]
            {
                new StepRequest("Step 1", null, DueOffsetHours: 0m),
            });

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task CreateWorkflow_WithNullDueOffsetHours_SucceedsWithNullOnStep()
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
            Name:        "No SLA Workflow",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps: new[]
            {
                new StepRequest("Step 1", null, DueOffsetHours: null),
            });

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        saved!.Steps.Single().DueOffsetHours.Should().BeNull();
    }
}
