using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Domain.Entities;

/// <summary>
/// An immutable record of a single observable event in a workflow instance's lifecycle.
/// Activity events are append-only — they are never updated or soft-deleted.
/// <para>
/// <see cref="Payload"/> is a JSON object (serialised as a string) that carries
/// event-specific details, e.g. the step name for a StepCompleted event.
/// </para>
/// </summary>
public sealed class WorkflowActivityEvent
{
    public Guid               Id                 { get; private init; }
    public Guid               WorkflowInstanceId { get; private init; }
    public Guid               TenantId           { get; private init; }
    public ActivityEventType  EventType          { get; private init; }
    public Guid               ActorUserId        { get; private init; }

    /// <summary>
    /// JSON payload carrying event-specific context, e.g.
    /// <c>{"stepName":"Sign-off","reason":"Power outage"}</c>.
    /// Null when the event has no extra context beyond the actor and timestamp.
    /// </summary>
    public string?  Payload    { get; private init; }
    public DateTime OccurredAt { get; private init; }

    // ── EF Core parameterless constructor ─────────────────────────────────
    private WorkflowActivityEvent() { }

    public static WorkflowActivityEvent Create(
        Guid              workflowInstanceId,
        Guid              tenantId,
        ActivityEventType eventType,
        Guid              actorUserId,
        string?           payload    = null,
        DateTime?         occurredAt = null)
        => new()
        {
            Id                 = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            TenantId           = tenantId,
            EventType          = eventType,
            ActorUserId        = actorUserId,
            Payload            = payload,
            OccurredAt         = occurredAt ?? DateTime.UtcNow,
        };
}
