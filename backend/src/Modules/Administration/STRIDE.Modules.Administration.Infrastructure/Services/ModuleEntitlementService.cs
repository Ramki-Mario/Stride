using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Infrastructure.Services;

/// <summary>
/// Checks per-tenant module entitlements by reading <c>TenantSettings.EnabledModules</c>.
/// Null/empty means all modules are enabled — preserving backward compatibility.
/// </summary>
internal sealed class ModuleEntitlementService : IModuleEntitlementService
{
    private readonly ITenantSettingsRepository _repo;

    public ModuleEntitlementService(ITenantSettingsRepository repo) => _repo = repo;

    public async Task<bool> IsModuleEnabledAsync(
        Guid tenantId, string moduleName, CancellationToken ct = default)
    {
        var settings = await _repo.GetByTenantIdAsync(tenantId, ct);
        return settings is null || settings.IsModuleEnabled(moduleName);
    }
}
