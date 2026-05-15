using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;

internal sealed class WorkflowDefinitionRepository
    : TenantAwareRepository<WorkflowDefinition, WorkflowsDbContext>,
      IWorkflowDefinitionRepository
{
    public WorkflowDefinitionRepository(WorkflowsDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Query
            .Include(d => d.Steps)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(CancellationToken ct = default)
        => await Query
            .Include(d => d.Steps)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await Query.AnyAsync(d => d.Name == name, ct);

    public async Task AddAsync(WorkflowDefinition definition, CancellationToken ct = default)
        => await Context.WorkflowDefinitions.AddAsync(definition, ct);

    public void Update(WorkflowDefinition definition)
        => Context.WorkflowDefinitions.Update(definition);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
