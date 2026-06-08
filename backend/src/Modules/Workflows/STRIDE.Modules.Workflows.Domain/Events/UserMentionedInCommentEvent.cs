using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

/// <summary>
/// Raised for each user whose @handle appears in a newly posted comment.
/// Consumed by the Notifications module to create an in-app mention notification.
/// Self-mentions (author == mentioned user) are never emitted.
/// </summary>
public sealed record UserMentionedInCommentEvent(
    Guid   CommentId,
    Guid   WorkflowInstanceId,
    string WorkflowName,
    Guid   TenantId,
    Guid   AuthorId,
    Guid   MentionedUserId) : IDomainEvent;
