using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Repository for <see cref="SharedWorkflowLink"/> aggregates. All methods are tenant-scoped.
/// </summary>
public interface ISharedWorkflowLinkRepository
{
    Task<SharedWorkflowLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the non-revoked links for a workflow instance, newest first.
    /// Expired-but-not-revoked links are included so the manager can see and clean them up.
    /// </summary>
    Task<IReadOnlyList<SharedWorkflowLink>> ListActiveByInstanceAsync(
        Guid instanceId, CancellationToken cancellationToken = default);

    Task AddAsync(SharedWorkflowLink link, CancellationToken cancellationToken = default);

    void Update(SharedWorkflowLink link);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
