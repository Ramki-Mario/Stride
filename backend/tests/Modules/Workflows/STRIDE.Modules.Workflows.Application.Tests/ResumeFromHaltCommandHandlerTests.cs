using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.ResumeFromHalt;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class ResumeFromHaltCommandHandlerTests
{
    private readonly IWorkflowInstanceRepository  _instances = Substitute.For<IWorkflowInstanceRepository>();
    private readonly ResumeFromHaltCommandHandler _sut;

    public ResumeFromHaltCommandHandlerTests()
    {
        _sut = new ResumeFromHaltCommandHandler(_instances, NullLogger<ResumeFromHaltCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenInstanceNotFound_ReturnsFailure()
    {
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(
            new ResumeFromHaltCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenHalted_ResumesToRunningAndSaves()
    {
        var instance = BuildHaltedInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new ResumeFromHaltCommand(instance.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowStatus.Running);
        _instances.Received(1).Update(instance);
        await _instances.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotHalted_ReturnsFailureFromDomainGuard()
    {
        var instance = BuildRunningInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new ResumeFromHaltCommand(instance.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Halted");
        await _instances.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance BuildRunningInstance()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "WF", null, Guid.NewGuid());
        def.AddStep("Step A", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }

    private static WorkflowInstance BuildHaltedInstance()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Halt WF", null, Guid.NewGuid());
        def.AddStep("Gate", null, stepType: StepType.Approval,
            rejectionHandling: RejectionHandling.HaltWorkflow);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.RejectStep(instance.Steps.Single().Id, Guid.NewGuid(), "Halted for test");
        instance.ClearDomainEvents();
        return instance;
    }
}
