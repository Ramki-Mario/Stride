using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Teams.Domain.Entities;
using STRIDE.Modules.Teams.Domain.Repositories;

namespace STRIDE.Modules.Teams.Infrastructure.Persistence;

internal sealed class TeamRepository : ITeamRepository
{
    private readonly TeamsDbContext _db;

    public TeamRepository(TeamsDbContext db) => _db = db;

    public Task<Team?> GetByIdAsync(Guid tenantId, Guid teamId, CancellationToken ct = default)
        => _db.Teams
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == teamId && !t.IsDeleted, ct);

    public Task<bool> ExistsByNameAsync(
        Guid tenantId, string name, Guid? excludeId = null, CancellationToken ct = default)
        => _db.Teams.AnyAsync(
            t => t.TenantId == tenantId
              && t.Name == name
              && !t.IsDeleted
              && (excludeId == null || t.Id != excludeId),
            ct);

    public async Task AddAsync(Team team, CancellationToken ct = default)
        => await _db.Teams.AddAsync(team, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
