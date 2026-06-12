using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.EventHandlers;
using STRIDE.Modules.Workflows.Application.Queries.GetActivityTimeline;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// US-173 activity timeline coverage: the four approval-gate event types must
/// each append an immutable activity record with the correct type and actor.
/// </summary>
public sealed class ApprovalActivityHandlerTests
{
    private readonly IWorkflowActivityRepository _activity = Substitute.For<IWorkflowActivityRepository>();

    [Fact]
    public async Task StepApproved_CreatesActivityEventWithApproverAsActor()
    {
        var ev      = new StepApprovedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Gate");
        var handler = new StepApprovedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<StepApprovedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType          == ActivityEventType.StepApproved &&
                e.WorkflowInstanceId == ev.WorkflowInstanceId          &&
                e.TenantId           == ev.TenantId                    &&
                e.ActorUserId        == ev.ApprovedBy),
            Arg.Any<CancellationToken>());
        await _activity.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StepRejected_CreatesActivityEventWithRejectorAsActor()
    {
        var ev      = new StepRejectedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Gate");
        var handler = new StepRejectedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<StepRejectedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType   == ActivityEventType.StepRejected &&
                e.ActorUserId == ev.RejectedBy),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StepsReverted_CreatesActivityEvent()
    {
        var reverted = new List<(Guid StepInstanceId, string StepName)>
        {
            (Guid.NewGuid(), "Intermediate"),
        };
        var ev = new StepsRevertedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Gate",
            TargetStepOrder: 0, reverted, Guid.NewGuid(), "Redo from step 1");
        var handler = new StepsRevertedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<StepsRevertedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType   == ActivityEventType.StepsReverted &&
                e.ActorUserId == ev.RejectedBy),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WorkflowHalted_CreatesActivityEvent()
    {
        var ev      = new WorkflowHaltedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var handler = new WorkflowHaltedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<WorkflowHaltedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType          == ActivityEventType.WorkflowHalted &&
                e.WorkflowInstanceId == ev.WorkflowInstanceId            &&
                e.ActorUserId        == ev.HaltedBy),
            Arg.Any<CancellationToken>());
    }
}
