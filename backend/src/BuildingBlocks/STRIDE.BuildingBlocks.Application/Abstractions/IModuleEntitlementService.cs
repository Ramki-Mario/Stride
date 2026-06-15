namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Checks whether a given module is enabled for a tenant.
/// Null / empty entitlement list means ALL modules are enabled (backward-compatible default).
/// </summary>
public interface IModuleEntitlementService
{
    Task<bool> IsModuleEnabledAsync(Guid tenantId, string moduleName, CancellationToken ct = default);
}
