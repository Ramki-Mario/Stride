using FluentAssertions;
using NSubstitute;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.EventHandlers;
using STRIDE.Modules.Workflows.Application.Queries.GetActivityTimeline;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class ActivityEventHandlerTests
{
    private readonly IWorkflowActivityRepository _activity  = Substitute.For<IWorkflowActivityRepository>();
    private readonly IWorkflowInstanceRepository _instances = Substitute.For<IWorkflowInstanceRepository>();

    // ── WorkflowStartedActivityHandler ───────────────────────────────────────

    [Fact]
    public async Task WorkflowStarted_CreatesActivityEventWithCorrectType()
    {
        var ev      = new WorkflowStartedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "WF", Guid.NewGuid());
        var handler = new WorkflowStartedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<WorkflowStartedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType          == ActivityEventType.WorkflowStarted &&
                e.WorkflowInstanceId == ev.WorkflowInstanceId             &&
                e.TenantId           == ev.TenantId                       &&
                e.ActorUserId        == ev.StartedBy),
            Arg.Any<CancellationToken>());
    }

    // ── WorkflowCompletedActivityHandler ─────────────────────────────────────

    [Fact]
    public async Task WorkflowCompleted_CreatesActivityEvent()
    {
        var ev      = new WorkflowCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var handler = new WorkflowCompletedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<WorkflowCompletedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e => e.EventType == ActivityEventType.WorkflowCompleted),
            Arg.Any<CancellationToken>());
    }

    // ── WorkflowCancelledActivityHandler ─────────────────────────────────────

    [Fact]
    public async Task WorkflowCancelled_CreatesActivityEvent()
    {
        var ev      = new WorkflowCancelledEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var handler = new WorkflowCancelledActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<WorkflowCancelledEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e => e.EventType == ActivityEventType.WorkflowCancelled),
            Arg.Any<CancellationToken>());
    }

    // ── StepAssignedActivityHandler ───────────────────────────────────────────

    [Fact]
    public async Task StepAssigned_SerializesStepNameIntoPayload()
    {
        var ev      = new StepAssignedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                                            Guid.NewGuid(), Guid.NewGuid(), "Inspection");
        var handler = new StepAssignedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<StepAssignedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType == ActivityEventType.StepAssigned &&
                e.Payload != null && e.Payload.Contains("Inspection")),
            Arg.Any<CancellationToken>());
    }

    // ── StepCompletedActivityHandler ──────────────────────────────────────────

    [Fact]
    public async Task StepCompleted_CreatesActivityEventWithActorAsCompletedBy()
    {
        var completedBy = Guid.NewGuid();
        var ev          = new StepCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                                                 completedBy, "Approval");
        var handler = new StepCompletedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<StepCompletedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType   == ActivityEventType.StepCompleted &&
                e.ActorUserId == completedBy),
            Arg.Any<CancellationToken>());
    }

    // ── StepFailedActivityHandler ─────────────────────────────────────────────

    [Fact]
    public async Task StepFailed_SerializesReasonIntoPayload()
    {
        var ev      = new StepFailedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                                          "Power outage", Guid.NewGuid(), "Installation");
        var handler = new StepFailedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<StepFailedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType == ActivityEventType.StepFailed &&
                e.Payload   != null                         &&
                e.Payload.Contains("Power outage")          &&
                e.Payload.Contains("Installation")),
            Arg.Any<CancellationToken>());
    }

    // ── StepSkippedActivityHandler ────────────────────────────────────────────

    [Fact]
    public async Task StepSkipped_CreatesActivityEvent()
    {
        var ev      = new StepSkippedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sign-off");
        var handler = new StepSkippedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<StepSkippedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType == ActivityEventType.StepSkipped &&
                e.Payload != null && e.Payload.Contains("Sign-off")),
            Arg.Any<CancellationToken>());
    }

    // ── StepOverdueActivityHandler ────────────────────────────────────────────

    [Fact]
    public async Task StepOverdue_UsesStartedByAsActor()
    {
        var startedBy = Guid.NewGuid();
        var ev        = new StepOverdueEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                                             Guid.NewGuid(), startedBy, "Checkout");
        var handler   = new StepOverdueActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<StepOverdueEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType   == ActivityEventType.StepOverdue &&
                e.ActorUserId == startedBy),
            Arg.Any<CancellationToken>());
    }

    // ── CommentPostedActivityHandler ──────────────────────────────────────────

    [Fact]
    public async Task CommentPosted_CreatesActivityEventWithAuthorAsActor()
    {
        var authorId = Guid.NewGuid();
        var ev       = new CommentPostedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), authorId, "Nice work");
        var handler  = new CommentPostedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<CommentPostedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType   == ActivityEventType.CommentPosted &&
                e.ActorUserId == authorId),
            Arg.Any<CancellationToken>());
    }

    // ── AttachmentUploadedActivityHandler ─────────────────────────────────────

    [Fact]
    public async Task AttachmentUploaded_SerializesFileNameIntoPayload()
    {
        var uploadedBy = Guid.NewGuid();
        var ev         = new AttachmentUploadedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), uploadedBy, "report.pdf");
        var handler    = new AttachmentUploadedActivityHandler(_activity);

        await handler.Handle(new DomainEventNotification<AttachmentUploadedEvent>(ev), CancellationToken.None);

        await _activity.Received(1).AddAsync(
            Arg.Is<WorkflowActivityEvent>(e =>
                e.EventType   == ActivityEventType.AttachmentUploaded &&
                e.ActorUserId == uploadedBy                           &&
                e.Payload     != null && e.Payload.Contains("report.pdf")),
            Arg.Any<CancellationToken>());
    }

    // ── GetActivityTimelineQueryHandler ──────────────────────────────────────

    [Fact]
    public async Task GetActivityTimeline_WhenInstanceNotFound_ReturnsFailure()
    {
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var handler = new GetActivityTimelineQueryHandler(_activity, _instances);
        var query   = new GetActivityTimelineQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetActivityTimeline_ReturnsMappedDtos()
    {
        var instance   = BuildInstance();
        var instanceId = instance.Id;

        _instances.GetByIdAsync(instanceId, Arg.Any<CancellationToken>())
                  .Returns(instance);

        var actEv = WorkflowActivityEvent.Create(
            instanceId, instance.TenantId,
            ActivityEventType.WorkflowStarted, Guid.NewGuid());

        IReadOnlyList<WorkflowActivityEvent> items = new[] { actEv };
        _activity.ListByInstanceAsync(instanceId, 1, 20, true, Arg.Any<CancellationToken>())
                 .Returns((items, 1));

        var handler = new GetActivityTimelineQueryHandler(_activity, _instances);
        var query   = new GetActivityTimelineQuery(instanceId, Page: 1, PageSize: 20, Ascending: true);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].EventType.Should().Be((int)ActivityEventType.WorkflowStarted);
        result.Value.Items[0].EventTypeLabel.Should().Be("Workflow started");
        result.Value.Items[0].Description.Should().Be("Workflow was started.");
    }

    [Fact]
    public async Task GetActivityTimeline_ClampsPageBelowOne()
    {
        var instance = BuildInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
                  .Returns(instance);

        IReadOnlyList<WorkflowActivityEvent> empty = Array.Empty<WorkflowActivityEvent>();
        _activity.ListByInstanceAsync(instance.Id, 1, 20, true, Arg.Any<CancellationToken>())
                 .Returns((empty, 0));

        var handler = new GetActivityTimelineQueryHandler(_activity, _instances);
        var query   = new GetActivityTimelineQuery(instance.Id, Page: -3, PageSize: 20, Ascending: true);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetActivityTimeline_ClampsPageSizeToHundred()
    {
        var instance = BuildInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
                  .Returns(instance);

        IReadOnlyList<WorkflowActivityEvent> empty = Array.Empty<WorkflowActivityEvent>();
        _activity.ListByInstanceAsync(instance.Id, 1, 100, true, Arg.Any<CancellationToken>())
                 .Returns((empty, 0));

        var handler = new GetActivityTimelineQueryHandler(_activity, _instances);
        var query   = new GetActivityTimelineQuery(instance.Id, Page: 1, PageSize: 500, Ascending: true);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(100);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance BuildInstance()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Test", null, Guid.NewGuid());
        def.AddStep("Step 1", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
