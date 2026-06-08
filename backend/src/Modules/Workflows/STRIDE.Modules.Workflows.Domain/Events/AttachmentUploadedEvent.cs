using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

/// <summary>
/// Raised when a file is attached to a workflow instance or one of its steps.
/// Consumed by the activity-timeline handler (US-166).
/// </summary>
public sealed record AttachmentUploadedEvent(
    Guid    AttachmentId,
    Guid    WorkflowInstanceId,
    Guid?   StepInstanceId,
    Guid    TenantId,
    Guid    UploadedBy,
    string  FileName) : IDomainEvent;
