namespace STRIDE.Modules.Notifications.Domain.Enums;

public enum NotificationType
{
    WorkflowStarted,
    WorkflowCompleted,
    WorkflowFailed,
    StepAssigned,
    StepCompleted,
    StepOverdue,
    WorkflowSlaBreached,
    SystemAlert,
    InvoiceDraftCreated,
    Mentioned,
}
