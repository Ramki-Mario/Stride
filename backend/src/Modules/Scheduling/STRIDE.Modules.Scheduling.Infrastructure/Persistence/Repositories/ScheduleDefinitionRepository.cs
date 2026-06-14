using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Scheduling.Application.Abstractions;
using STRIDE.Modules.Scheduling.Domain.Entities;

namespace STRIDE.Modules.Scheduling.Infrastructure.Persistence.Repositories;

internal sealed class ScheduleDefinitionRepository
    : TenantAwareRepository<ScheduleDefinition, SchedulingDbContext>,
      IScheduleDefinitionRepository
{
    public ScheduleDefinitionRepository(SchedulingDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task AddAsync(ScheduleDefinition schedule, CancellationToken ct = default)
        => await Context.ScheduleDefinitions.AddAsync(schedule, ct);

    public Task<ScheduleDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Query.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<ScheduleDefinition>> GetAllAsync(CancellationToken ct = default)
        => await Query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
