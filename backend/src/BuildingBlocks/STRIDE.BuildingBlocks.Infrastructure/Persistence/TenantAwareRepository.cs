using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.BuildingBlocks.Infrastructure.Persistence;

public abstract class TenantAwareRepository<TEntity, TContext>
    where TEntity : AuditableEntity
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly ITenantContext Tenant;

    protected TenantAwareRepository(TContext context, ITenantContext tenant)
    {
        Context = context;
        Tenant  = tenant;
    }

    /// <summary>
    /// All repository queries MUST use this as the base — TenantId + soft-delete filters applied automatically.
    /// </summary>
    protected IQueryable<TEntity> Query =>
        Context.Set<TEntity>()
               .Where(e => e.TenantId == Tenant.TenantId && !e.IsDeleted);
}
