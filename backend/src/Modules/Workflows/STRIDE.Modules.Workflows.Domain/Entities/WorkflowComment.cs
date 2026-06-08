using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

/// <summary>
/// A comment posted on a workflow instance by a team member.
/// Supports soft-delete (body replaced with "(deleted)") and author-only edits.
/// </summary>
public sealed class WorkflowComment : AuditableEntity
{
    public Guid   WorkflowInstanceId { get; private set; }

    /// <summary>
    /// The comment text.  When soft-deleted this is replaced with the sentinel
    /// string <c>"(deleted)"</c> so thread structure is preserved.
    /// </summary>
    public string Body               { get; private set; } = string.Empty;

    /// <summary>Non-null once the author has edited the comment at least once.</summary>
    public DateTime? EditedAt        { get; private set; }

    private WorkflowComment() { }

    /// <summary>
    /// Creates and returns a new comment. Raises <see cref="CommentPostedEvent"/>.
    /// </summary>
    public static WorkflowComment Create(
        Guid workflowInstanceId,
        Guid tenantId,
        Guid authorId,
        string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new WorkflowDomainException("Comment body cannot be empty.");

        if (body.Length > 4000)
            throw new WorkflowDomainException("Comment body cannot exceed 4 000 characters.");

        var now = DateTime.UtcNow;
        var comment = new WorkflowComment
        {
            Id                 = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            TenantId           = tenantId,
            Body               = body.Trim(),
            CreatedAt          = now,
            UpdatedAt          = now,
            CreatedBy          = authorId,
        };

        comment.RaiseDomainEvent(new CommentPostedEvent(
            comment.Id,
            workflowInstanceId,
            tenantId,
            authorId,
            comment.Body));

        return comment;
    }

    /// <summary>
    /// Replaces the comment body. Only the original author may edit.
    /// </summary>
    public void Edit(string newBody, Guid editorId)
    {
        if (IsDeleted)
            throw new WorkflowDomainException("A deleted comment cannot be edited.");

        if (editorId != CreatedBy)
            throw new WorkflowDomainException("Only the original author may edit this comment.");

        if (string.IsNullOrWhiteSpace(newBody))
            throw new WorkflowDomainException("Comment body cannot be empty.");

        if (newBody.Length > 4000)
            throw new WorkflowDomainException("Comment body cannot exceed 4 000 characters.");

        Body      = newBody.Trim();
        EditedAt  = DateTime.UtcNow;
        UpdatedAt = EditedAt.Value;
    }

    /// <summary>
    /// Soft-deletes the comment. The body is replaced with <c>"(deleted)"</c> so the thread
    /// structure is preserved. Either the original author or a manager may delete.
    /// </summary>
    public void SoftDelete(Guid deleterId, bool isManager)
    {
        if (IsDeleted) return; // idempotent

        if (deleterId != CreatedBy && !isManager)
            throw new WorkflowDomainException("Only the author or a manager may delete this comment.");

        Body      = "(deleted)";
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
