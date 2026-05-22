using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Application.Abstractions;

public interface ITenantSettingsRepository
{
    Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(TenantSettings settings, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
