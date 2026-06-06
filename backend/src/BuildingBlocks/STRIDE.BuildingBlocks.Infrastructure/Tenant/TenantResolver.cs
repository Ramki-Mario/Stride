namespace STRIDE.BuildingBlocks.Infrastructure.Tenant;

/// <summary>
/// Resolves a TenantId from an email address.
/// Corporate domains map directly; generic domains (gmail, outlook, etc.) use UserTenantMapping.
/// Full implementation lives in the Identity module Infrastructure layer.
/// This class defines the contract used by the BFF auth flow.
/// </summary>
public interface ITenantResolver
{
    Task<Guid?> ResolveAsync(string email, CancellationToken cancellationToken = default);
}
