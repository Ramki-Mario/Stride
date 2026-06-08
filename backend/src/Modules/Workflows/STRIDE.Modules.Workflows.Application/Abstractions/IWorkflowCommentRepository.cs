using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Repository for <see cref="WorkflowComment"/> aggregates.
/// All read methods are automatically scoped to the current tenant.
/// </summary>
public interface IWorkflowCommentRepository
{
    /// <summary>Returns a comment by primary key (tenant-scoped, includes deleted).</summary>
    Task<WorkflowComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of comments for a given workflow instance, ordered oldest-first.
    /// Deleted comments are included so thread structure is preserved.
    /// </summary>
    Task<(IReadOnlyList<WorkflowComment> Items, int TotalCount)> ListByInstanceAsync(
        Guid instanceId,
        int  page,
        int  pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Stages a new comment for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(WorkflowComment comment, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
