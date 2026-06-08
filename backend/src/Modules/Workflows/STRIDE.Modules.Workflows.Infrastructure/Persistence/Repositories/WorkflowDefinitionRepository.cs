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

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Query
            .Include(d => d.Steps)
                .ThenInclude(s => s.Fields)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Query
            .Include(d => d.Steps)
                .ThenInclude(s => s.Fields)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        => await Query.AnyAsync(d => d.Name == name, cancellationToken);

    public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        => await Context.WorkflowDefinitions.AddAsync(definition, cancellationToken);

    public void Update(WorkflowDefinition definition)
        => Context.WorkflowDefinitions.Update(definition);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}
