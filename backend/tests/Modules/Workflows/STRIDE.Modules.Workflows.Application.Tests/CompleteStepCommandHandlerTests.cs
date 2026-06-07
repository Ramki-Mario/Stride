using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.CompleteStep;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class CompleteStepCommandHandlerTests
{
    private readonly IWorkflowInstanceRepository _repo =
        Substitute.For<IWorkflowInstanceRepository>();

    private readonly CompleteStepCommandHandler _sut;

    public CompleteStepCommandHandlerTests()
    {
        _sut = new CompleteStepCommandHandler(
            _repo,
            NullLogger<CompleteStepCommandHandler>.Instance);
    }

    // ── Instance not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceNotFound_ReturnsFailure()
    {
        // Arrange
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var command = new CompleteStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Success without billable items ────────────────────────────────────────

    [Fact]
    public async Task Handle_WithNoItems_CompletesStepAndPersists()
    {
        // Arrange
        var (instance, stepId) = BuildRunningInstance();
        _repo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
            .Returns(instance);

        var command = new CompleteStepCommand(instance.Id, stepId, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repo.Received(1).Update(instance);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Success with billable items ───────────────────────────────────────────

    [Fact]
    public async Task Handle_WithBillableItems_CompletesStepWithItems()
    {
        // Arrange
        var (instance, stepId) = BuildRunningInstance();
        _repo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
            .Returns(instance);

        var command = new CompleteStepCommand(
            instance.Id,
            stepId,
            Guid.NewGuid(),
            BillableItems: new[]
            {
                new BillableItemInput("Labour",        2.0m, 85.00m, BillableUnit.Hours),
                new BillableItemInput("Parts",         3.0m, 12.00m, BillableUnit.Each),
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var step = instance.Steps.Single(s => s.Id == stepId);
        step.BillableItems.Should().HaveCount(2);
        step.BillableItems.Sum(b => b.LineTotal).Should().Be(2.0m * 85.00m + 3.0m * 12.00m);
    }

    [Fact]
    public async Task Handle_WithBillableItems_PersistsInstance()
    {
        // Arrange
        var (instance, stepId) = BuildRunningInstance();
        _repo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
            .Returns(instance);

        var command = new CompleteStepCommand(
            instance.Id,
            stepId,
            Guid.NewGuid(),
            BillableItems: new[] { new BillableItemInput("Labour", 1.0m, 50.00m, BillableUnit.Hours) });

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repo.Received(1).Update(instance);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Domain exception propagation ──────────────────────────────────────────

    [Fact]
    public async Task Handle_WithInvalidBillableItem_ReturnsFailure()
    {
        // Arrange — empty description should trigger domain exception
        var (instance, stepId) = BuildRunningInstance();
        _repo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
            .Returns(instance);

        var command = new CompleteStepCommand(
            instance.Id,
            stepId,
            Guid.NewGuid(),
            BillableItems: new[] { new BillableItemInput("", 1.0m, 50.00m, BillableUnit.Hours) });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("description");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (WorkflowInstance instance, Guid firstStepId) BuildRunningInstance()
    {
        var definition = BuildActiveDefinition(2);
        var instance   = WorkflowInstance.Start(definition, Guid.NewGuid());
        instance.ClearDomainEvents();
        var stepId = instance.Steps.OrderBy(s => s.Order).First().Id;
        return (instance, stepId);
    }

    private static WorkflowDefinition BuildActiveDefinition(int stepCount)
    {
        var def = WorkflowDefinition.Create(
            Guid.NewGuid(),
            "Test Workflow",
            null,
            Guid.NewGuid());

        for (var i = 0; i < stepCount; i++)
            def.AddStep($"Step {i + 1}", null);

        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();
        return def;
    }
}
