using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;

/// <summary>
/// Append-only repository for <see cref="WorkflowActivityEvent"/>.
/// Does NOT extend <see cref="STRIDE.BuildingBlocks.Infrastructure.Persistence.TenantAwareRepository{T,TContext}"/>
/// because <see cref="WorkflowActivityEvent"/> is not an <c>AuditableEntity</c>
/// (no IsDeleted / no soft-delete). Tenant scoping is applied manually.
/// </summary>
internal sealed class WorkflowActivityRepository : IWorkflowActivityRepository
{
    private readonly WorkflowsDbContext _context;
    private readonly ITenantContext     _tenant;

    public WorkflowActivityRepository(WorkflowsDbContext context, ITenantContext tenant)
    {
        _context = context;
        _tenant  = tenant;
    }

    public async Task AddAsync(
        WorkflowActivityEvent activityEvent,
        CancellationToken cancellationToken = default)
        => await _context.WorkflowActivityEvents.AddAsync(activityEvent, cancellationToken);

    public async Task<(IReadOnlyList<WorkflowActivityEvent> Items, int TotalCount)>
        ListByInstanceAsync(
            Guid instanceId,
            int  page,
            int  pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.WorkflowActivityEvents
            .Where(e => e.TenantId           == _tenant.TenantId
                     && e.WorkflowInstanceId == instanceId);

        baseQuery = ascending
            ? baseQuery.OrderBy(e => e.OccurredAt)
            : baseQuery.OrderByDescending(e => e.OccurredAt);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
