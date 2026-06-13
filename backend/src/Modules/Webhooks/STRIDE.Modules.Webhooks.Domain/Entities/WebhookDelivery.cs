using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Enums;
using STRIDE.Modules.Webhooks.Domain.Events;
using STRIDE.Modules.Webhooks.Domain.Exceptions;

namespace STRIDE.Modules.Webhooks.Domain.Entities;

/// <summary>
/// Records one outbound delivery attempt for a <see cref="WebhookSubscription"/>.
///
/// Retry schedule (exponential-ish, 4 attempts total):
///   Attempt 1 — immediate (called by the dispatcher on the domain-event path)
///   Attempt 2 — +1 minute
///   Attempt 3 — +5 minutes
///   Attempt 4 — +30 minutes
/// After the 4th failure the delivery is marked <see cref="DeliveryStatus.Exhausted"/> and
/// a <see cref="WebhookDeliveryExhaustedEvent"/> is raised so an admin notification can be sent.
/// </summary>
public sealed class WebhookDelivery : AuditableEntity
{
    private static readonly int[] RetryDelaysMinutes = [1, 5, 30];
    private const int MaxAttempts = 4;

    public Guid           WebhookSubscriptionId { get; private set; }
    public string         EventType             { get; private set; } = string.Empty;
    public string         Payload               { get; private set; } = string.Empty;
    public string         Url                   { get; private set; } = string.Empty;
    public DeliveryStatus Status                { get; private set; }
    public int?           ResponseCode          { get; private set; }
    public string?        ResponseBody          { get; private set; }
    public int            AttemptCount          { get; private set; }
    public DateTime?      NextAttemptAt         { get; private set; }
    public DateTime?      LastAttemptAt         { get; private set; }

    // EF Core constructor
    private WebhookDelivery() { }

    // ── Factory ─────────────────────────────────────────────────────────────

    public static WebhookDelivery Create(
        Guid tenantId, Guid subscriptionId, string eventType, string url, string payload)
    {
        var now = DateTime.UtcNow;
        return new WebhookDelivery
        {
            Id                    = Guid.NewGuid(),
            TenantId              = tenantId,
            WebhookSubscriptionId = subscriptionId,
            EventType             = eventType,
            Url                   = url,
            Payload               = payload,
            Status                = DeliveryStatus.Pending,
            AttemptCount          = 0,
            CreatedAt             = now,
            UpdatedAt             = now,
            CreatedBy             = Guid.Empty,  // system-generated
        };
    }

    // ── Outcome recording ───────────────────────────────────────────────────

    public void RecordSuccess(int responseCode, string? responseBody)
    {
        Status        = DeliveryStatus.Success;
        ResponseCode  = responseCode;
        ResponseBody  = Truncate(responseBody, 1000);
        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;
        NextAttemptAt = null;
        UpdatedAt     = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed delivery attempt. Returns <c>true</c> when the delivery is now
    /// <see cref="DeliveryStatus.Exhausted"/> (all retries consumed).
    /// </summary>
    public bool RecordFailure(int? responseCode, string? responseBody)
    {
        ResponseCode  = responseCode;
        ResponseBody  = Truncate(responseBody ?? "Timeout or connection error", 1000);
        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;
        UpdatedAt     = DateTime.UtcNow;

        if (AttemptCount >= MaxAttempts)
        {
            Status        = DeliveryStatus.Exhausted;
            NextAttemptAt = null;
            RaiseDomainEvent(new WebhookDeliveryExhaustedEvent(Id, TenantId, WebhookSubscriptionId, EventType));
            return true;
        }

        Status        = DeliveryStatus.Failed;
        // AttemptCount is 1-based; index into the delay array is attempt-1 (attempt 1 just failed → wait RetryDelaysMinutes[0])
        NextAttemptAt = DateTime.UtcNow.AddMinutes(RetryDelaysMinutes[AttemptCount - 1]);
        return false;
    }

    // ── Manual retry ─────────────────────────────────────────────────────────

    public void ScheduleManualRetry()
    {
        if (Status != DeliveryStatus.Exhausted)
            throw new WebhookDomainException("Only exhausted deliveries can be manually retried.");

        Status        = DeliveryStatus.Pending;
        AttemptCount  = 0;
        NextAttemptAt = DateTime.UtcNow;
        UpdatedAt     = DateTime.UtcNow;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? Truncate(string? s, int max) =>
        s is null ? null : s.Length <= max ? s : s[..max];
}
