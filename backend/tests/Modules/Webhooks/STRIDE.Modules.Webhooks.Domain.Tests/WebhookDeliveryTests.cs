using FluentAssertions;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Enums;
using STRIDE.Modules.Webhooks.Domain.Events;

namespace STRIDE.Modules.Webhooks.Domain.Tests;

public sealed class WebhookDeliveryTests
{
    private static readonly Guid   Tenant       = Guid.NewGuid();
    private static readonly Guid   Subscription = Guid.NewGuid();
    private const string           EventType    = WebhookEventTypes.WorkflowInstanceCompleted;
    private const string           Url          = "https://example.com/hooks";
    private const string           Payload      = """{"event":"workflow.instance.completed"}""";

    private static WebhookDelivery NewDelivery() =>
        WebhookDelivery.Create(Tenant, Subscription, EventType, Url, Payload);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsFieldsCorrectly()
    {
        var d = NewDelivery();

        d.TenantId.Should().Be(Tenant);
        d.WebhookSubscriptionId.Should().Be(Subscription);
        d.EventType.Should().Be(EventType);
        d.Url.Should().Be(Url);
        d.Payload.Should().Be(Payload);
        d.Status.Should().Be(DeliveryStatus.Pending);
        d.AttemptCount.Should().Be(0);
        d.ResponseCode.Should().BeNull();
        d.NextAttemptAt.Should().BeNull();
        d.LastAttemptAt.Should().BeNull();
    }

    // ── RecordSuccess ─────────────────────────────────────────────────────────

    [Fact]
    public void RecordSuccess_SetsStatusSuccessAndIncrementsAttempt()
    {
        var d = NewDelivery();

        d.RecordSuccess(200, "OK");

        d.Status.Should().Be(DeliveryStatus.Success);
        d.ResponseCode.Should().Be(200);
        d.AttemptCount.Should().Be(1);
        d.LastAttemptAt.Should().NotBeNull();
        d.NextAttemptAt.Should().BeNull();
    }

    [Fact]
    public void RecordSuccess_TruncatesLongBody()
    {
        var d    = NewDelivery();
        var body = new string('x', 1500);

        d.RecordSuccess(200, body);

        d.ResponseBody!.Length.Should().Be(1000);
    }

    // ── RecordFailure — retry scheduling ──────────────────────────────────────

    [Fact]
    public void RecordFailure_FirstAttempt_SetsFailedAndSchedulesRetry()
    {
        var d = NewDelivery();
        var before = DateTime.UtcNow;

        var exhausted = d.RecordFailure(503, "Service unavailable");

        exhausted.Should().BeFalse();
        d.Status.Should().Be(DeliveryStatus.Failed);
        d.AttemptCount.Should().Be(1);
        d.NextAttemptAt.Should().BeOnOrAfter(before.AddMinutes(1));
        d.NextAttemptAt.Should().BeOnOrBefore(before.AddMinutes(1).AddSeconds(5));
    }

    [Fact]
    public void RecordFailure_SecondAttempt_SchedulesLongerDelay()
    {
        var d = NewDelivery();
        var before = DateTime.UtcNow;

        d.RecordFailure(503, "err");  // attempt 1 → wait 1 min
        var exhausted = d.RecordFailure(503, "err"); // attempt 2 → wait 5 min

        exhausted.Should().BeFalse();
        d.AttemptCount.Should().Be(2);
        d.NextAttemptAt.Should().BeOnOrAfter(before.AddMinutes(5));
    }

    [Fact]
    public void RecordFailure_ThirdAttempt_SchedulesLongestDelay()
    {
        var d = NewDelivery();
        var before = DateTime.UtcNow;

        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err");
        var exhausted = d.RecordFailure(503, "err"); // attempt 3 → wait 30 min

        exhausted.Should().BeFalse();
        d.AttemptCount.Should().Be(3);
        d.NextAttemptAt.Should().BeOnOrAfter(before.AddMinutes(30));
    }

    [Fact]
    public void RecordFailure_FourthAttempt_SetsExhaustedAndRaisesEvent()
    {
        var d = NewDelivery();

        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err");
        var exhausted = d.RecordFailure(503, "err"); // attempt 4 → exhausted

        exhausted.Should().BeTrue();
        d.Status.Should().Be(DeliveryStatus.Exhausted);
        d.AttemptCount.Should().Be(4);
        d.NextAttemptAt.Should().BeNull();

        d.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryExhaustedEvent);
        var evt = (WebhookDeliveryExhaustedEvent)d.DomainEvents.Single();
        evt.TenantId.Should().Be(Tenant);
        evt.SubscriptionId.Should().Be(Subscription);
        evt.EventType.Should().Be(EventType);
    }

    [Fact]
    public void RecordFailure_NullBody_UsesDefaultMessage()
    {
        var d = NewDelivery();

        d.RecordFailure(null, null);

        d.ResponseBody.Should().NotBeNullOrEmpty();
    }

    // ── ScheduleManualRetry ───────────────────────────────────────────────────

    [Fact]
    public void ScheduleManualRetry_WhenExhausted_ResetsToPending()
    {
        var d = NewDelivery();
        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err"); // exhausted

        d.ScheduleManualRetry();

        d.Status.Should().Be(DeliveryStatus.Pending);
        d.AttemptCount.Should().Be(0);
        d.NextAttemptAt.Should().BeOnOrAfter(DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void ScheduleManualRetry_WhenNotExhausted_Throws()
    {
        var d = NewDelivery();
        d.RecordFailure(503, "err"); // just Failed, not Exhausted

        var act = () => d.ScheduleManualRetry();

        act.Should().Throw<Exceptions.WebhookDomainException>()
            .WithMessage("*exhausted*");
    }
}
