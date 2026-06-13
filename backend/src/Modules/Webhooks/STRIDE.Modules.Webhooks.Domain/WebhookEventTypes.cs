namespace STRIDE.Modules.Webhooks.Domain;

/// <summary>
/// The catalog of well-defined events a tenant can subscribe a webhook to.
///
/// These string keys are the contract shared with external systems: they appear
/// in the <c>EventTypes</c> array of a subscription and as the <c>event</c> field
/// of every dispatched payload. They are intentionally stable, lower-cased,
/// dot-namespaced identifiers so integrators can pattern-match on a prefix
/// (e.g. all <c>workflow.*</c> events). New events are added here as the platform
/// grows; removing one is a breaking change for subscribers.
/// </summary>
public static class WebhookEventTypes
{
    public const string WorkflowInstanceStarted   = "workflow.instance.started";
    public const string WorkflowInstanceCompleted = "workflow.instance.completed";
    public const string WorkflowStepCompleted     = "workflow.step.completed";
    public const string WorkflowStepOverdue       = "workflow.step.overdue";
    public const string InvoiceCreated            = "invoice.created";
    public const string InvoiceSent               = "invoice.sent";
    public const string UserInvited               = "user.invited";

    /// <summary>The full set of subscribable event keys, for validation and UI population.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        WorkflowInstanceStarted,
        WorkflowInstanceCompleted,
        WorkflowStepCompleted,
        WorkflowStepOverdue,
        InvoiceCreated,
        InvoiceSent,
        UserInvited,
    };

    public static bool IsKnown(string eventType) => All.Contains(eventType);
}
