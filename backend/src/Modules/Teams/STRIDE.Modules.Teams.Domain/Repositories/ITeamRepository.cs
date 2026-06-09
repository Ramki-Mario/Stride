using STRIDE.Modules.Teams.Domain.Entities;

namespace STRIDE.Modules.Teams.Domain.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid tenantId, Guid teamId, CancellationToken ct = default);
    Task<bool>  ExistsByNameAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken ct = default);
    Task        AddAsync(Team team, CancellationToken ct = default);
    Task        SaveChangesAsync(CancellationToken ct = default);
}
