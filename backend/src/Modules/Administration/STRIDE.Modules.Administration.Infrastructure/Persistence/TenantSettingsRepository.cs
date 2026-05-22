using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Infrastructure.Persistence;

internal sealed class TenantSettingsRepository : ITenantSettingsRepository
{
    private readonly AdministrationDbContext _db;

    public TenantSettingsRepository(AdministrationDbContext db) => _db = db;

    public Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => _db.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

    public async Task AddAsync(TenantSettings settings, CancellationToken ct = default)
        => await _db.TenantSettings.AddAsync(settings, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
