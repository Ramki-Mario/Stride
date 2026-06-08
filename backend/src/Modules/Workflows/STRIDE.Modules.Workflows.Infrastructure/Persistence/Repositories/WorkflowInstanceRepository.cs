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
    public async Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Query
            .Include(i => i.Steps)
                .ThenInclude(s => s.BillableItems)
            .Include(i => i.Steps)
                .ThenInclude(s => s.FieldValues)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowInstance>> GetByDefinitionIdAsync(
        Guid definitionId,
        CancellationToken cancellationToken = default)
        => await Query
            .Include(i => i.Steps)
                .ThenInclude(s => s.BillableItems)
            .Include(i => i.Steps)
                .ThenInclude(s => s.FieldValues)
            .Where(i => i.WorkflowDefinitionId == definitionId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkflowInstance>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Query
            .Include(i => i.Steps)
                .ThenInclude(s => s.BillableItems)
            .Include(i => i.Steps)
                .ThenInclude(s => s.FieldValues)
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
        => await Context.WorkflowInstances.AddAsync(instance, cancellationToken);

    public void Update(WorkflowInstance instance)
        => Context.WorkflowInstances.Update(instance);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}
