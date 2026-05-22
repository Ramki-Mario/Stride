namespace STRIDE.Modules.Administration.Application.Abstractions;

/// <summary>
/// Cross-schema read: resolves a tenant's registered display name from the
/// <c>identity.Tenants</c> table so that <c>TenantSettings.DisplayName</c>
/// can be seeded correctly on first access.
/// </summary>
public interface ITenantNameResolver
{
    /// <summary>
    /// Returns the tenant's <c>Name</c> column value, or an empty string if not found.
    /// </summary>
    Task<string> ResolveAsync(Guid tenantId, CancellationToken ct = default);
}
