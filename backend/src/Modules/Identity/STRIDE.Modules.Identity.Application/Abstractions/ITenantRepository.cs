using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

/// <summary>
/// Repository for the Tenant aggregate root.
/// NOTE: Tenant lookups are intentionally cross-tenant — a Tenant record
/// is owned by itself (TenantId == Id), so queries here do NOT filter by
/// ITenantContext.TenantId. This allows lookups at login time before a
/// tenant context has been established.
/// </summary>
public interface ITenantRepository
{
    /// <summary>Returns a tenant by its primary key. No tenant-context filter applied.</summary>
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a tenant by its unique slug. No tenant-context filter applied.</summary>
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Returns true if an active tenant with this slug already exists.</summary>
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Stages a new tenant for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>Marks an existing tenant as modified. Caller must call SaveChangesAsync.</summary>
    void Update(Tenant tenant);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
