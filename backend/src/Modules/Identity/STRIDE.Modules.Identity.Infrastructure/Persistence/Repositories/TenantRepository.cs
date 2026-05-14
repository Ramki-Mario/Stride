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

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await TenantQuery
            .Include(t => t.DomainMappings)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await TenantQuery
            .Include(t => t.DomainMappings)
            .FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default)
        => await TenantQuery.AnyAsync(t => t.Slug == slug, ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
        => await Context.Tenants.AddAsync(tenant, ct);

    public void Update(Tenant tenant)
        => Context.Tenants.Update(tenant);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
