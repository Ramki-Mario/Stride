namespace STRIDE.Modules.Workflows.Domain.Enums;

/// <summary>
/// Discriminates every kind of event recorded in the workflow instance activity timeline.
/// Values are persisted as integers — never renumber existing entries.
/// </summary>
public enum ActivityEventType
{
    // ── Workflow lifecycle ────────────────────────────────────────────────
    WorkflowStarted     = 1,
    WorkflowCompleted   = 2,
    WorkflowCancelled   = 3,
    WorkflowSlaBreached = 4,
    WorkflowPaused      = 5,
    WorkflowResumed     = 6,

    // ── Step lifecycle ────────────────────────────────────────────────────
    StepAssigned  = 10,
    StepCompleted = 12,
    StepFailed    = 13,
    StepSkipped   = 14,
    StepOverdue   = 15,

    // ── Content ───────────────────────────────────────────────────────────
    CommentPosted      = 20,
    AttachmentUploaded = 30,
}
