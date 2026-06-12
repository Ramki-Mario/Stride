namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Shared contract for real-time dashboard update messages flowing from the Host
/// (where domain events are raised) to the BFF (which owns the browser-facing
/// SignalR hub) over a Redis pub/sub channel.
///
/// Lives in BuildingBlocks so both processes reference the same channel name and
/// payload shape without a cross-module dependency — same reasoning as
/// <see cref="AuditActions"/> (ADR-017).
/// </summary>
public static class DashboardUpdates
{
    /// <summary>Redis pub/sub channel the Host publishes to and the BFF subscribes to.</summary>
    public const string Channel = "stride:dashboard:updates";

    // ── Event type constants (sent verbatim to the browser) ──────────────────
    public const string StepCompleted     = "stepCompleted";
    public const string StepAssigned      = "stepAssigned";
    public const string StepOverdue       = "stepOverdue";
    public const string WorkflowCompleted = "workflowCompleted";
    public const string InvoiceCreated    = "invoiceCreated";
}

/// <summary>
/// Payload published on <see cref="DashboardUpdates.Channel"/>.
/// Serialized with System.Text.Json defaults on both sides.
/// </summary>
public sealed record DashboardUpdateMessage(Guid TenantId, string EventType);
