using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

/// <summary>
/// Raised when a comment is posted on a workflow instance.
/// Consumed by the activity-timeline handler (US-166) and the @mention handler (US-167).
/// </summary>
public sealed record CommentPostedEvent(
    Guid CommentId,
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid AuthorId,
    string Body) : IDomainEvent;
