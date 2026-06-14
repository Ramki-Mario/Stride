using STRIDE.Modules.Scheduling.Domain.Entities;

namespace STRIDE.Modules.Scheduling.Application.Abstractions;

public interface IScheduleDefinitionRepository
{
    Task AddAsync(ScheduleDefinition schedule, CancellationToken ct = default);
    Task<ScheduleDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduleDefinition>> GetAllAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
