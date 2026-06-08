using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class WorkflowActivityEventTests
{
    private static readonly Guid InstanceId = Guid.NewGuid();
    private static readonly Guid TenantId   = Guid.NewGuid();
    private static readonly Guid ActorId    = Guid.NewGuid();

    [Fact]
    public void Create_WithRequiredFields_SetsAllProperties()
    {
        var evt = WorkflowActivityEvent.Create(
            InstanceId, TenantId, ActivityEventType.WorkflowStarted, ActorId);

        evt.Id.Should().NotBeEmpty();
        evt.WorkflowInstanceId.Should().Be(InstanceId);
        evt.TenantId.Should().Be(TenantId);
        evt.EventType.Should().Be(ActivityEventType.WorkflowStarted);
        evt.ActorUserId.Should().Be(ActorId);
        evt.Payload.Should().BeNull();
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithPayload_StoresPayloadString()
    {
        var payload = "{\"stepName\":\"Review\"}";

        var evt = WorkflowActivityEvent.Create(
            InstanceId, TenantId, ActivityEventType.StepCompleted, ActorId, payload);

        evt.Payload.Should().Be(payload);
    }

    [Fact]
    public void Create_WithExplicitOccurredAt_UsesProvidedTimestamp()
    {
        var occurredAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = WorkflowActivityEvent.Create(
            InstanceId, TenantId, ActivityEventType.WorkflowSlaBreached, ActorId,
            occurredAt: occurredAt);

        evt.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void Create_WithoutOccurredAt_UsesCurrentUtcTime()
    {
        var before = DateTime.UtcNow;
        var evt    = WorkflowActivityEvent.Create(InstanceId, TenantId, ActivityEventType.CommentPosted, ActorId);
        var after  = DateTime.UtcNow;

        evt.OccurredAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_EachCall_ProducesUniqueId()
    {
        var evt1 = WorkflowActivityEvent.Create(InstanceId, TenantId, ActivityEventType.StepAssigned, ActorId);
        var evt2 = WorkflowActivityEvent.Create(InstanceId, TenantId, ActivityEventType.StepAssigned, ActorId);

        evt1.Id.Should().NotBe(evt2.Id);
    }

    [Fact]
    public void Create_AttachmentEvent_WithPayload_SetsAll()
    {
        var payload = "{\"fileName\":\"report.pdf\"}";

        var evt = WorkflowActivityEvent.Create(
            InstanceId, TenantId, ActivityEventType.AttachmentUploaded, ActorId, payload);

        evt.EventType.Should().Be(ActivityEventType.AttachmentUploaded);
        evt.Payload.Should().Be(payload);
    }
}
