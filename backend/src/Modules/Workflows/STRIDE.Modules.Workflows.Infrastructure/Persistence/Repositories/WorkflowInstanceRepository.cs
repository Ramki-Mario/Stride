using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;

internal sealed class WorkflowInstanceRepository
    : TenantAwareRepository<WorkflowInstance, WorkflowsDbContext>,
      IWorkflowInstanceRepository
{
    public WorkflowInstanceRepository(WorkflowsDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    /// <summary>
    /// Always eagerly loads step instances — required for all step-level operations
    /// performed through the WorkflowInstance aggregate root.
    /// </summary>
    public async Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Query
            .Include(i => i.Steps)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<WorkflowInstance>> GetByDefinitionIdAsync(
        Guid definitionId,
        CancellationToken ct = default)
        => await Query
            .Include(i => i.Steps)
            .Where(i => i.WorkflowDefinitionId == definitionId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkflowInstance>> GetAllAsync(CancellationToken ct = default)
        => await Query
            .Include(i => i.Steps)
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(WorkflowInstance instance, CancellationToken ct = default)
        => await Context.WorkflowInstances.AddAsync(instance, ct);

    public void Update(WorkflowInstance instance)
        => Context.WorkflowInstances.Update(instance);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
