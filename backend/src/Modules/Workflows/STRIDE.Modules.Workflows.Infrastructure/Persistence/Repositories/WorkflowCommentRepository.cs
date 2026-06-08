using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;

internal sealed class WorkflowCommentRepository
    : TenantAwareRepository<WorkflowComment, WorkflowsDbContext>,
      IWorkflowCommentRepository
{
    public WorkflowCommentRepository(WorkflowsDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    /// <summary>
    /// Fetches by ID without filtering out soft-deleted rows — the caller (Edit / Delete
    /// handlers) needs to see deleted comments to return the correct error message.
    /// Tenant-scoping is still applied.
    /// </summary>
    public async Task<WorkflowComment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await Context.WorkflowComments
            .Where(c => c.TenantId == Tenant.TenantId && c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(IReadOnlyList<WorkflowComment> Items, int TotalCount)> ListByInstanceAsync(
        Guid instanceId,
        int  page,
        int  pageSize,
        CancellationToken cancellationToken = default)
    {
        // Include deleted comments so thread structure is preserved.
        var baseQuery = Context.WorkflowComments
            .Where(c => c.TenantId          == Tenant.TenantId
                     && c.WorkflowInstanceId == instanceId)
            .OrderBy(c => c.CreatedAt);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }

    public async Task AddAsync(
        WorkflowComment comment,
        CancellationToken cancellationToken = default)
        => await Context.WorkflowComments.AddAsync(comment, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}
