using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Append-only repository for <see cref="WorkflowActivityEvent"/> records.
/// All query methods are automatically scoped to the current tenant.
/// </summary>
public interface IWorkflowActivityRepository
{
    /// <summary>Stages a new activity event for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(WorkflowActivityEvent activityEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of activity events for a given workflow instance.
    /// </summary>
    /// <param name="ascending">When true, returns oldest-first; when false, newest-first.</param>
    Task<(IReadOnlyList<WorkflowActivityEvent> Items, int TotalCount)> ListByInstanceAsync(
        Guid instanceId,
        int  page,
        int  pageSize,
        bool ascending            = true,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
