namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface ITenantResolver
{
    /// <summary>
    /// Resolves the TenantId for the given email address.
    /// Corporate domain emails are matched via TenantDomainMapping.
    /// Generic domain emails (gmail, outlook, etc.) fall back to UserTenantMapping.
    /// Returns null if no tenant can be resolved.
    /// </summary>
    Task<Guid?> ResolveFromEmailAsync(string email, CancellationToken cancellationToken = default);
}
