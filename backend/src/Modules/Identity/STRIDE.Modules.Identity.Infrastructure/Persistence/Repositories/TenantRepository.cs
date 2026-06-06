using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Tenant lookups are cross-tenant by design — a Tenant record owns itself
/// (TenantId == Id), and lookups happen at login time before ITenantContext
/// is populated. This repository bypasses the ITenantContext filter and
/// queries Tenants directly by Id or Slug with only the soft-delete filter.
/// </summary>
internal sealed class TenantRepository : TenantAwareRepository<Tenant, IdentityDbContext>, ITenantRepository
{
    // Intentionally bypasses base Query (which filters by ITenantContext.TenantId).
    // Tenant records are global — each tenant owns itself.
    private IQueryable<Tenant> TenantQuery =>
        Context.Tenants
               .Where(t => !t.IsDeleted);

    public TenantRepository(IdentityDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await TenantQuery
            .Include(t => t.DomainMappings)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await TenantQuery
            .Include(t => t.DomainMappings)
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await TenantQuery.AnyAsync(t => t.Slug == slug, cancellationToken);

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        => await Context.Tenants.AddAsync(tenant, cancellationToken);

    public void Update(Tenant tenant)
        => Context.Tenants.Update(tenant);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}
