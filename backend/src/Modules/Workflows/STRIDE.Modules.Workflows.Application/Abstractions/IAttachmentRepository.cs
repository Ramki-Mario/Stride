using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Repository for <see cref="Attachment"/> entities.
/// All queries are automatically scoped to the current tenant via the
/// <c>TenantAwareRepository</c> base class (TenantId + IsDeleted filter).
/// </summary>
public interface IAttachmentRepository
{
    Task<Attachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Attachment>> GetByStepInstanceIdAsync(
        Guid stepInstanceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Attachment>> GetByWorkflowInstanceIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Attachment attachment, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
