using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Infrastructure.Persistence;

internal sealed class TenantSettingsRepository : ITenantSettingsRepository
{
    private readonly AdministrationDbContext _db;

    public TenantSettingsRepository(AdministrationDbContext db) => _db = db;

    public Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _db.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

    public async Task AddAsync(TenantSettings settings, CancellationToken cancellationToken = default)
        => await _db.TenantSettings.AddAsync(settings, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
